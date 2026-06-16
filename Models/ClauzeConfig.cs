using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ActAditionalPlugin.Models
{
    public class CampClauza
    {
        [JsonProperty("label")] public string Label { get; set; }
        [JsonProperty("placeholder")] public string Placeholder { get; set; }
        [JsonProperty("ordine")] public int Ordine { get; set; }
        [JsonProperty("tipCamp")] public string TipCamp { get; set; }

        public CampClauza() { Label = string.Empty; Placeholder = string.Empty; TipCamp = "text"; }
    }

    public class ClauzeActAditional
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("titlu")] public string Titlu { get; set; }
        [JsonProperty("textClauza")] public string TextClauza { get; set; }
        [JsonProperty("textDinamic")] public string TextDinamic { get; set; }
        [JsonProperty("campuri")] public List<CampClauza> Campuri { get; set; }
        [JsonProperty("activ")] public bool Activ { get; set; }

        public ClauzeActAditional()
        {
            Id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Titlu = string.Empty; TextClauza = string.Empty; TextDinamic = string.Empty;
            Campuri = new List<CampClauza>(); Activ = true;
        }
    }

    public class TipContract
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nume")] public string Nume { get; set; }
        [JsonProperty("activ")] public bool Activ { get; set; }
        [JsonProperty("clauze")] public List<ClauzeActAditional> Clauze { get; set; }

        public TipContract()
        {
            Id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Nume = string.Empty; Activ = true;
            Clauze = new List<ClauzeActAditional>();
        }
    }

    public class ClauzeConfig
    {
        [JsonProperty("tipuriContract")] public List<TipContract> TipuriContract { get; set; }
        [JsonProperty("tipSelectatId")] public string TipSelectatId { get; set; }

        public ClauzeConfig() { TipuriContract = new List<TipContract>(); TipSelectatId = string.Empty; }

        public TipContract GetTipSelectat()
        {
            var tip = TipuriContract.FirstOrDefault(t => t.Id == TipSelectatId && t.Activ);
            return tip ?? TipuriContract.FirstOrDefault(t => t.Activ);
        }
    }
}