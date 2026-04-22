using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public abstract class DecizieFormBase : DocumentFormBase
    {
        protected TextBox TxtCodInregistrare;
        protected DateTimePicker DtpDataDecizie;

        protected DecizieFormBase(DocumentModelBase model, string titlu)
            : base(model, titlu) { }

        protected TableLayoutPanel AddRandDecizie(Panel parent, int top)
        {
            TxtCodInregistrare = MakeInput("ex. 26001/1");
            DtpDataDecizie = MakeDtp();
            var tbl = AddRow(parent, top, new[] { 40, 60 });
            AddLabeledInput(tbl, 0, "Cod înregistrare", TxtCodInregistrare, required: true);
            AddLabeledInput(tbl, 1, "Data decizie", DtpDataDecizie);
            return tbl;
        }

        protected void PopulateDecizie(DecizieModelBase m)
        {
            FillAngajator(m);
            m.CodInregistrare = GetText(TxtCodInregistrare);
            m.DataDecizie = GetDate(DtpDataDecizie);
        }

        protected bool ValidateDecizie()
        {
            if (!RequireText(TxtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(TxtCodInregistrare)) return false;
            return true;
        }
    }

    public abstract class DecizieCuCerereBase : DecizieFormBase
    {
        protected TextBox TxtNrCerere;
        protected DateTimePicker DtpDataCerere;

        protected DecizieCuCerereBase(DocumentModelBase model, string titlu)
            : base(model, titlu) { }

        protected TableLayoutPanel AddRandCerere(Panel parent, int top)
        {
            TxtNrCerere = MakeInput("ex. 175");
            DtpDataCerere = MakeDtp();
            var tbl = AddRow(parent, top, new[] { 40, 60 });
            AddLabeledInput(tbl, 0, "Nr. cerere", TxtNrCerere, required: true);
            AddLabeledInput(tbl, 1, "Data cerere", DtpDataCerere);
            return tbl;
        }

        protected void PopulateCerere(DecizieModelCuCerere m)
        {
            PopulateDecizie(m);
            m.NrCerere = GetText(TxtNrCerere);
            m.DataCerere = GetDate(DtpDataCerere);
        }

        protected bool ValidateCerere()
        {
            if (!ValidateDecizie()) return false;
            return RequireText(TxtNrCerere, "Nr. cerere");
        }
    }
}