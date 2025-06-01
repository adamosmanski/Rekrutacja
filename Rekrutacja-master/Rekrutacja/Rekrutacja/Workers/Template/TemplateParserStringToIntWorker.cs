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
using static Rekrutacja.Workers.Template.TemplateCalculatorWorker;
[assembly: Worker(typeof(TemplateParserStringToIntWorker), typeof(Pracownicy))]

namespace Rekrutacja.Workers.Template
{
    public static class StringToIntParser
    {
        public static int Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Wejściowy string jest pusty lub null.");

            input = input.Trim();
            int result = 0;
            int sign = 1;
            int i = 0;

            if (input[0] == '-')
            {
                sign = -1;
                i++;
            }
            else if (input[0] == '+')
            {
                i++;
            }

            for (; i < input.Length; i++)
            {
                char c = input[i];

                if (c < '0' || c > '9')
                    throw new FormatException($"Nieprawidłowy znak: '{c}'");

                int digit = c - '0';

                if (sign == 1 && (result > (int.MaxValue - digit) / 10))
                    throw new OverflowException("Wartość przekracza zakres typu Int32.");
                if (sign == -1 && (result > (int.MaxValue - digit + 1) / 10))
                    throw new OverflowException("Wartość przekracza zakres typu Int32.");

                result = result * 10 + digit;
            }

            return sign * result;
        }
    }
    public class TemplateParserStringToIntWorker
    {
        
        [Context]
        public Context Cx { get; set; }
        [Context]
        public TemplateParserStringToIntParametry Parametry { get; set; }
        public class TemplateParserStringToIntParametry : ContextBase
        {
            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("A")]
            public string LiczbaA { get; set; }

            [Caption("B")]
            public string LiczbaB { get; set; }

            public TemplateParserStringToIntParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
                this.LiczbaA = "Liczba A : 5";
                this.LiczbaB = "Liczba B: 8";
            }

        }
        [Action("Parser", Description = "Prosty konwerter typów string na int", Priority = 10, Mode = ActionMode.ReadOnlySession, Icon = ActionIcon.Accept, Target = ActionTarget.ToolbarWithText)]
        public void ParsujStringToInt()
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
                wynik = KonwertujStringNaInt(Parametry.LiczbaA, Parametry.LiczbaB);
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

                        pracownikZSesja.Features["Parser_String_To_Int"] = wynik;
                        pracownikZSesja.Features["DataObliczen"] = Parametry.DataObliczen;
                    }

                    trans.CommitUI();
                }

                nowaSesja.Save();
            }
        }
        private int KonwertujStringNaInt(string a, string b)
        {
            string wyczyszczoneA = new string(a.Where(c => char.IsDigit(c) || c == '-' || c == '+').ToArray());
            string wyczyszczoneB = new string(b.Where(c => char.IsDigit(c)).ToArray());

            string polaczony = wyczyszczoneA + wyczyszczoneB;

            return StringToIntParser.Parse(polaczony);
        }
    }
}
