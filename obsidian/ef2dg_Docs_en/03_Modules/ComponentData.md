# ComponentData

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/ComponentData.md)

`ComponentData` is the DTO of a single component. `TypeName` identifies the class by name in the `ef2dg` namespace; simple public properties are spread across the `FloatValues`, `BoolValues`, `IntValues`, `StringValues` and `VectorValues` dictionaries.

Loading creates an instance through reflection and assigns the values to properties that have a public setter. Properties of other types, private fields and read-only members are not saved automatically.
