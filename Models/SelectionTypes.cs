namespace ActAditionalPlugin.Models
{
    /// <summary>Rezultatul selectiei din SelectorDialog — document sau PV.</summary>
    public abstract class DocumentSelection { }

    public sealed class DocSelection : DocumentSelection
    {
        public TipDocument Tip { get; }
        public DocSelection(TipDocument tip) { Tip = tip; }
    }

    public sealed class PvSelection : DocumentSelection
    {
        public TipPV Tip { get; }
        public PvSelection(TipPV tip) { Tip = tip; }
    }
}