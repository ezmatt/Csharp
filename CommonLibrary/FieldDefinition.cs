public class FieldDefinition
{
    public string FieldName { get; set; }
    public string Type { get; set; }
    public bool IsMandatory { get; set; }
    public List<string> PossibleValues { get; set; }
    public string Comments { get; set; }
    public string DataExample { get; set; }
}
