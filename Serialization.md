# Serializing the model and ink data

## Serializing

[SaveModelAsync()](FamilyNotes/App.xaml.cs#L307) serializes the `Model` and saves the ink data.

The [Model](FamilyNotes/Model.cs) class is serialized by using the [DataContractJsonSerializer](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.json.datacontractjsonserializer.aspx) class. The `Model` is annotated with the [System.Runtime.Serialization.DataContract](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.datacontractattribute.aspx) attribute. This attribute indicates that the type can be serialized using a serializer such as the `DataContractJsonSerializer`.
Not all of the members of the `Model` class need to be serialized. Members to serialize are marked with the [System.Runtime.Serialization.DataMember attribute](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.datamemberattribute.aspx). This defines the contract that the `DataContractJsonSerializer` uses to serialize the object.

First, the `Model` is serialized using the `DataContractJsonSerializer`, but the ink can't be serialized by using one of the standard serializers. Although the `InkStrokecontainer` has a [SaveAsync()](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/windows.ui.input.inking.inkstrokecontainer.saveasync.aspx) method, that method isn't suited for saving consecutive `InkStrokeContainer` objects to a file because when we try to load an `InkStrokeContainer` using [LoadAsync](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/windows.ui.input.inking.inkstrokecontainer.loadasync.aspx), it is unable to distinguish between the multiple `InkStrokeContainer` in the stream.

We didn't want to have individual backing files for every note to store the Ink Serialized Format (ISF) data. That would have meant writing code to manage the backing files for the notes. So, instead, the code extracts the ink stroke data from each Note's `InkStrokecontainer`, combines it into one `InkStrokecontainer`, and saves it to an ISF file.

In order to identify which ink strokes belong to which `InkStrokecontainer`, the number of ink strokes per container are preserved. When the ink stroke data is loaded, because the notes are read in the same order they were written out, we can identify how many ink strokes in the container belong to each note and restore them.


## Deserializing

[LoadModelAsync()](FamilyNotes/App.xaml.cs#L367) deserializes the `Model` and ink data.

First, the `Model` is deserialized using the `DataContractJsonSerializer`. It is important to note that when a type is deserialized, the `DataContractJsonSerializer` does not call the constructor or field initializers for the deserialized object. In the case of the `Model`, this is a problem for the `_family` and `_stickyNotes` fields because they need to represent valid collection objects before the deserialization process tries to add deserialized content to those collections.
To initialize the fields before deserialization tries to access them, the [System.Runtime.Serialization.OnDeserializing](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.ondeserializingattribute.aspx) attribute is applied to the [OnDeserializing](FamilyNotes/Model.cs#L214) method. This method is called when the`Model`is being deserialized. It in turn calls [Initialize()](FamilyNotes/Model.cs#L57), which constructs the collections for the `_family` and `_stickyNotes` fields. Initialization code for the object is centralized in `Initialize()` so that it can also be called from the `Model` constructor.

After the `Model` is deserialized, the code iterates through the restored notes. For each note, the code reads the number of ink strokes that belong to its `InkCanvas`. Once the number of ink strokes per note is read, the combined ink stroke data is loaded. Then, each note extracts the number of ink strokes that belong to it from the combined `InkStrokecontainer`.

It is important to call [Windows.UI.Input.Inking.Clone](https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.input.inking.inkstroke.clone.aspx) to copy an `InkStroke` from the combined `InkStrokeContainer` to the individual note's `InkStrokeContainer` because an `InkStroke` can't belong to multiple `InkStrokeContainers` at the same time.
