using Rekrutacja.Workers.Template;
using Soneta.Business;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Kasa;
using Soneta.Types;
using Syncfusion.XPS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Rejetracja Workera - Pierwszy TypeOf określa jakiego typu ma być wyświetlany Worker, Drugi parametr wskazuje na jakim Typie obiektów będzie wyświetlany Worker
[assembly: Worker(typeof(TemplateWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateWorker
    {
        
        //Aby parametry działały prawidłowo dziedziczymy po klasie ContextBase
        public class TemplateWorkerParametry : ContextBase
        {
            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("A")]
            public int LiczbaA { get; set; }

            [Caption("B")]
            public int LiczbaB { get; set; }

            [Caption("Operacja")]
            [Description("Dostępne operacje: +, -, *, /")]
            public string Operacja { get; set; }
            [Caption("Figura")]
            [Description("Wybierz figurę do obliczenia pola powierzchni")]
            public TemplateWorkerParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
                this.LiczbaA = 5;
                this.LiczbaB = 8;
                this.Operacja = "+";
            }
        }
        //Obiekt Context jest to pudełko które przechowuje Typy danych, aktualnie załadowane w aplikacji
        //Atrybut Context pobiera z "Contextu" obiekty które aktualnie widzimy na ekranie
        [Context]
        public Context Cx { get; set; }
        //Pobieramy z Contextu parametry, jeżeli nie ma w Context Parametrów mechanizm sam utworzy nowy obiekt oraz wyświetli jego formatkę
        [Context]
        public TemplateWorkerParametry Parametry { get; set; }
        //Atrybut Action - Wywołuje nam metodę która znajduje się poniżej
        [Action("Kalkulator",
           Description = "Prosty kalkulator ",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public void WykonajAkcje()
        {
            //Włączenie Debug, aby działał należy wygenerować DLL w trybie DEBUG
            DebuggerSession.MarkLineAsBreakPoint();
            //Pobieranie danych z Contextu

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

            double wynik;
            try
            {
                wynik = Oblicz(Parametry.LiczbaA, Parametry.LiczbaB, Parametry.Operacja);
            }
            catch (DivideByZeroException)
            {
                throw new InvalidOperationException("Nie można dzielić przez zero.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas obliczeń: {ex.Message}");
            }
            //Modyfikacja danych
            //Aby modyfikować dane musimy mieć otwartą sesję, któa nie jest read only
            using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                //Otwieramy Transaction aby można było edytować obiekt z sesji
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    foreach (var pracownik in pracownicy)
                    {
                        var pracownikZSesja = nowaSesja.Get(pracownik);
                        if (pracownikZSesja == null)
                            continue;

                        pracownikZSesja.Features["Wynik"] = wynik;
                        pracownikZSesja.Features["DataObliczen"] = Parametry.DataObliczen;
                    }
                    //Zatwierdzamy zmiany wykonane w sesji
                    trans.CommitUI();
                }
                //Zapisujemy zmiany
                nowaSesja.Save();
            }
        }
        private double Oblicz(int a, int b, string operacja)
        {
            switch (operacja)
            {
                case "+":
                    return a + b;
                case "-":
                    return a - b;
                case "*":
                    return a * b;
                case "/":
                    if (b == 0)
                        throw new DivideByZeroException("Nie można dzielić przez zero.");
                    return (double)a / b;
                default:
                    throw new ArgumentException("Nieprawidłowa operacja. Użyj +, -, * lub /.");
            }
        }

        
    }
}