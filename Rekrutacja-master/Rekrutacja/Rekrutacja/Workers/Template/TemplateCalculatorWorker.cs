using Rekrutacja.Workers.Template;
using Soneta.Business;
using Soneta.Kadry;
using Soneta.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rekrutacja.Workers.Template.TemplateWorker;

[assembly: Worker(typeof(TemplateCalculatorWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateCalculatorWorker
    {
        [Context]
        public Context Cx { get; set; }
        [Context]
        public TemplateCalculatorWorkerParametry Parametry { get; set; }
        public enum FiguraTyp
        {
            Kwadrat,
            Prostokat,
            Trojkat,
            Kolo
        }
        public class TemplateCalculatorWorkerParametry : ContextBase
        {
            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("A")]
            public int LiczbaA { get; set; }

            [Caption("B")]
            public int LiczbaB { get; set; }

            [Caption("Figura")]
            [Description("Wybierz figurę do obliczenia pola powierzchni")]
            public FiguraTyp Figura { get; set; }
            public TemplateCalculatorWorkerParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
                this.LiczbaA = 5;
                this.LiczbaB = 8;
                this.Figura = FiguraTyp.Kwadrat;
            }
           
        }
        [Action("Kalkulator figur geometrycznych", Description = "Prosty kalkulator", Priority = 10, Mode = ActionMode.ReadOnlySession, Icon = ActionIcon.Accept, Target = ActionTarget.ToolbarWithText)]
        public void WykonajAkcjeFigurGeometrycznych()
        {
            var pracownicy = new List<Pracownik>();

            if (Cx.Get(out Pracownik pojedynczyPracownik))
            {
                pracownicy.Add(pojedynczyPracownik);
            }
            else if (Cx.Get(out Row[] zaznaczoneWiersze))
            {
                foreach (var row in zaznaczoneWiersze)
                {
                    if (row is Pracownik pracownik)
                        pracownicy.Add(pracownik);
                }
            }

            if (pracownicy.Count == 0)
                throw new InvalidOperationException("Nie zaznaczono żadnych pracowników.");

            int wynik;
            try
            {
                wynik = ObliczPole(Parametry.LiczbaA, Parametry.LiczbaB, Parametry.Figura);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas obliczeń: {ex.Message}");
            }

            using (Session nowaSesja = Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    foreach (var pracownik in pracownicy)
                    {
                        var pracownikZSesja = nowaSesja.Get(pracownik);
                        if (pracownikZSesja == null)
                            continue;

                        pracownikZSesja.Features["Pola_Figury"] = wynik;
                        pracownikZSesja.Features["DataObliczen"] = Parametry.DataObliczen;
                    }

                    trans.CommitUI();
                }

                nowaSesja.Save();
            }
        }

        private int ObliczPole(int a, int b, FiguraTyp figura)
        {
            switch (figura)
            {
                case FiguraTyp.Kwadrat:
                    return a * a;
                case FiguraTyp.Prostokat:
                    return a * b;
                case FiguraTyp.Trojkat:
                    return (a * b) / 2;
                case FiguraTyp.Kolo:
                    return (int)(Math.PI * a * a);
                default:
                    throw new ArgumentException("Nieznany typ figury.");
            }
        }
    }
}
