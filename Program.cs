using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNF
{
    class Program
    {
        static void Main(string[] args)
        {
                string choose = "";
                while (choose != "1" && choose != "2")
                {
                    Console.WriteLine("Выберите режим работы:");
                    Console.WriteLine("1) Ручной ввод логической функции.");
                    Console.WriteLine("2) Замена содержимого файлов в папке models.");
                    choose = Console.ReadLine();                
            }
            if (choose == "1")
                {
                    while (true)
                    {
                        Console.WriteLine("Введите выражение: ");
                        Console.WriteLine("Код: \t\n" + OPZ.Translate(Console.ReadLine()));
                        OPZ.allGood = true;
                    }
                }
                else
            {
                string path = Directory.GetCurrentDirectory();
                List<string> models = Directory.GetFiles(path + "\\models", "*.mod").ToList<string>();
                foreach (var model in models)
                {
                    StreamReader sr = new StreamReader(model);
                    string text = sr.ReadToEnd();
                    sr.Close();
                    while (text.Contains("booleanFunction"))
                    {
                        int startIndex = text.IndexOf("booleanFunction");
                        int endIndex = 0;
                        for (int i = startIndex; i < text.Length; i++)
                            if (text[i] == '}')
                            {
                                endIndex = i;
                                break;
                            }
                        if (endIndex != 0)
                        {
                            int end = endIndex - 1;
                            int start = 0;
                            for (int i = startIndex + 15; i < endIndex; i++)
                                if (text[i] == '{')
                                {
                                    start = i + 1;
                                    break;
                                }
                            if (start != 0)
                            {
                                string input = text.Substring(start, end - start + 1);
                                string output = OPZ.Translate(input);
                                text = text.Replace(text.Substring(startIndex, endIndex - startIndex + 1), output);
                            }
                        }
                        StreamWriter sw = new StreamWriter(model, false, Encoding.UTF8);
                        sw.Write(text);
                        sw.Close();
                    }
                }
            }
        }
    }
    class OPZ
    {
        public static int subjectCount = 0;
        public static string output;
        static List<String> names;
        static bool[,] table;
        static bool[] funcVal;
        public static String sknf;
        public static string letters = "abcdefghijklmnopqrstuvwxyz";
        public static string digits = "0123456789";
        public static string[] blockList = { "var", "and", "or", "subject", "to", "maximize", "minimaze", "for", "sum", "binary", "integer", "symbolic", "default", "param", "set", "let" };
        static int n = 0;
        public static bool allGood = true;
        static public bool isCorrect(string name)
        {
            if (name == null)
                return false;
            if (letters.IndexOf(name[0]) == -1 && name[0] != '_')
                return false;
            foreach (var block in blockList)
            {
                if (name == block)
                    return false;
            }
            for (int i = 1; i < name.Length; i++)
                if (letters.IndexOf(name[i]) == -1 && name[i] != '_' && digits.IndexOf(name[i]) == -1)
                    return false;
            return true;
        }
        public static void Cleaning(ref String defInput)
        {
            defInput = defInput.Replace(" ", "");
            if (defInput.Contains("="))
            {
                if (defInput[defInput.Length - 2] != '=')
                {
                    allGood = false;
                    return;
                }
                else
                {
                    bool bad = defInput[defInput.Length - 1] == '0';
                    defInput = defInput.Remove(defInput.Length - 2, 2);
                    if (bad)
                        defInput = "!(" + defInput + ")";
                }
            }
        }
        public static string Translate(string input)
        {
            Cleaning(ref input);
            Tabling(GetExpression(input));
            if (allGood)
            {
                output = string.Empty;
                for (int j = 0; j < names.Count; j++)
                    output += "var " + names[j] + " binary;\t\n";
                for (int i = 0; i < n; i++)
                    if (!funcVal[i])
                    {
                        output += "s.t. sum" + (subjectCount++).ToString() + ":";
                        for (int j = 0; j < names.Count; j++)
                        {
                            if (table[i, j])
                                output += "(1-" + names[j] + ")+";
                            else output += names[j] + "+";
                        }
                        output = output.Remove(output.Length - 1, 1);
                        output += ">=1;\t\n";
                    }
                return output;
            }
            else return "Your expression doesnt support. Sorry!";
        }
        public static void SKNF()
        {
            sknf = string.Empty;
            for (int i = 0; i < n; i++)
                if (!funcVal[i])
                {
                    sknf += "(";
                    for (int j = 0; j < names.Count; j++)
                    {
                        if (table[i, j])
                            sknf += "!";
                        sknf += names[j] + "|";
                    }
                    sknf = sknf.Remove(sknf.Length - 1, 1);
                    sknf += ")&";
                }
            sknf = sknf.Remove(sknf.Length - 1, 1);
        }
        static public string GetExpression(string input)
        {
            names = new List<string>();
            string output = string.Empty;
            Stack<char> operStack = new Stack<char>();
            for (int i = 0; i < input.Length; i++)
            {
                if (IsOperator(input[i]))
                {
                    if (input[i] == '(')
                        operStack.Push(input[i]);
                    else if (input[i] == ')')
                    {
                        char s = operStack.Pop();
                        while (s != '(')
                        {
                            output += s.ToString() + ' ';
                            s = operStack.Pop();
                        }
                    }
                    else
                    {
                        if (operStack.Count > 0)
                            if (GetPriority(input[i]) <= GetPriority(operStack.Peek()))
                                output += operStack.Pop().ToString() + " ";
                        operStack.Push(char.Parse(input[i].ToString()));
                    }
                }
                else if (input[i] != '=')
                {
                    string s = string.Empty;
                    while (input[i] != '=' && !IsOperator(input[i]))
                    {
                        s += input[i++];
                        if (i == input.Length)
                            break;
                    }
                    output += s + " ";
                    if (!names.Contains(s) && s != "0" && s != "1")
                        if (isCorrect(s))
                            names.Add(s);
                        else allGood = false;
                    i--;
                }
            }
            while (operStack.Count > 0)
                output += operStack.Pop() + " ";
            output = output.Remove(output.Length - 1, 1);
            return output;
        }
        public static void Tabling(string OPZInput)
        {
            if (allGood)
            {
                n = (int)Math.Pow(2, names.Count);
                table = new bool[n, names.Count];
                funcVal = new bool[n];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < names.Count; j++)
                        table[i, j] = i / (int)Math.Pow(2, names.Count - 1 - j) % 2 == 1;
                for (int i = 0; i < n; i++)
                {
                    bool[] values = new bool[names.Count];
                    for (int k = 0; k < names.Count; k++)
                        values[k] = table[i, k];
                    funcVal[i] = Counting(OPZInput, values);
                }
            }
        }
        static public bool Counting(string OPZInput, bool[] values)
        {
            if (allGood)
            {
                string chgOPZ = string.Empty;
                string name = string.Empty;
                if (!IsOperator(OPZInput[OPZInput.Length - 1]))
                {
                    allGood = false;
                    return true;
                }
                else for (int i = 0; i < OPZInput.Length; i++)
                    {
                        int index = 0;
                        if (!IsOperator(OPZInput[i]) && OPZInput[i] != ' ')
                            name += OPZInput[i];
                        else
                        {
                            if (IsOperator(OPZInput[i]))
                                chgOPZ += OPZInput[i];
                            else
                            {
                                if (name.Length != 0)
                                {
                                    if (name == "0")
                                        chgOPZ += "0";
                                    else if (name == "1")
                                        chgOPZ += "1";
                                    else
                                    {
                                        for (int k = 1; k < names.Count; k++)
                                            if (names[k] == name)
                                                index = k;
                                        if (values[index])
                                            chgOPZ += "1";
                                        else chgOPZ += "0";
                                    }
                                    name = string.Empty;
                                }

                            }

                        }
                    }
                bool result = true;
                Stack<bool> temp = new Stack<bool>();
                for (int i = 0; i < chgOPZ.Length; i++)
                {
                    if (chgOPZ[i] == '!')
                    {
                        if (temp.Count != 0)
                        {
                            result = !temp.Pop();
                            temp.Push(result);
                        }
                        else
                        {
                            allGood = false;
                            return true;
                        }
                    }
                    else if (IsOperator(chgOPZ[i]))
                    {
                        bool a = true;
                        if (temp.Count != 0)
                            a = temp.Pop();
                        else
                        {
                            allGood = false;
                            return true;
                        }
                        bool b = true;
                        if (temp.Count != 0)
                            b = temp.Pop();
                        else
                        {
                            allGood = false;
                            return true;
                        }
                        switch (chgOPZ[i])
                        {
                            case '&': result = b & a; break;
                            case '|': result = b | a; break;
                            case (char)26: result = !b | a; break;
                            case '+': result = (b | a) & (!b | !a); break;
                            case (char)23: result = !b | !a; break;
                            case (char)29: result = (b | !a) & (!b | a); break;
                            case (char)25: result = !b & !a; break;
                            default: allGood = false; break;
                        }
                        temp.Push(result);
                    }
                    else temp.Push(chgOPZ[i] == '1' ? true : false);
                }
                return temp.Peek();
            }
            return true;
        }
        static private bool IsOperator(char c)
        {
            switch (c)
            {
                case '!': return true;
                case '&': return true;
                case '(': return true;
                case ')': return true;
                case '+': return true;
                case '|': return true;
                case (char)26: return true;
                case (char)25: return true;
                case (char)29: return true;
                case (char)23: return true;
                default: return false;
            }
        }
        static private byte GetPriority(char s)
        {
            switch (s)
            {
                case '(': return 0;
                case ')': return 1;
                case '|': return 2;
                case '&': return 3;
                case '+': return 4;
                case (char)29: return 5;
                case (char)23: return 6;
                case (char)26: return 7;
                case (char)25: return 8;
                case '!': return 9;
                default: return 10;
            }
        }
    }

}
