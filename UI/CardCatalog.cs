using System;
using System.Collections.Generic;
using System.Drawing;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    internal class CardItem
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Category { get; set; }
        public TipDocument? DocumentTip { get; set; }
        public bool IsPv { get; set; }
        public TipPV? PvTip { get; set; }
    }

    internal static class CardCatalog
    {
        // Category colors are defined in Theme

        internal static readonly CardItem[] DocumentCards = new[]
        {
            // Acte adiționale
            new CardItem { Title = "Act Adițional", Subtitle = "Modificare clauze CIM", Category = "Act", DocumentTip = TipDocument.ActAditional },

            // Decizii contract (all types)
            new CardItem { Title = "Creștere copil", Subtitle = "Suspendare CIM — art. 51 alin. 1", Category = "Decizie", DocumentTip = TipDocument.SuspendareCresterecopil },
            new CardItem { Title = "Creștere copil handicap", Subtitle = "Suspendare CIM — copil cu dizabilități", Category = "Decizie", DocumentTip = TipDocument.SuspendareCresterecopilHandicap },
            new CardItem { Title = "Absențe nemotivate", Subtitle = "Suspendare CIM — art. 51 alin. 2", Category = "Decizie", DocumentTip = TipDocument.SuspendareAbsenteNemotivate },
            new CardItem { Title = "Acordul părților", Subtitle = "Suspendare CIM — art. 54", Category = "Decizie", DocumentTip = TipDocument.SuspendareAcordParti },
            new CardItem { Title = "Suspendare + Încetare", Subtitle = "Suspendare și încetare suspendare", Category = "Decizie", DocumentTip = TipDocument.SuspendareSiIncetareSuspendare },
            new CardItem { Title = "Încetare suspendare", Subtitle = "Reluare activitate", Category = "Decizie", DocumentTip = TipDocument.IncetareSuspendare },
            new CardItem { Title = "Demisie", Subtitle = "Încetare CIM — art. 81", Category = "Decizie", DocumentTip = TipDocument.IncetareDemisie },
            new CardItem { Title = "Expirare termen", Subtitle = "Încetare CIM — art. 56", Category = "Decizie", DocumentTip = TipDocument.IncetareExpirare },
            new CardItem { Title = "Concediere disciplinară", Subtitle = "Încetare CIM — art. 61 lit. a", Category = "Decizie", DocumentTip = TipDocument.IncetareDisciplinar },

            // Procese verbale
            new CardItem { Title = "PV - Echipamente", Subtitle = "Procese verbale echipamente", Category = "PV", IsPv = true, PvTip = TipPV.Echipamente },
            new CardItem { Title = "PV - Electronice", Subtitle = "Procese verbale electronice", Category = "PV", IsPv = true, PvTip = TipPV.Electronice },
            new CardItem { Title = "PV - Autovehicul", Subtitle = "Procese verbale autovehicule", Category = "PV", IsPv = true, PvTip = TipPV.Autovehicul }
        };
    }
}
