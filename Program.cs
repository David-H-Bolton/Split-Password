// Author DH Bolton (C) Dice.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Rei.Random;
using System.IO;

namespace shared
{
    class Program
    {
        public static SFMT r = new SFMT();
        static string password;
        static List<string> partpw = new List<string>(5);
        static int[,] partpos = new int[5, 9];
        static int[,] testpos = new int[5, 9];
        static List<int> Counts = new List<int>(15);
        static List<string> Coded = new List<string>(5);
        static char[] newpasswordChars = new char[15];

        // Uses Mersenne Twister (SFMT) to generate a +ve rn in range 0-MaxLength-1
        public static int RandVal(int MaxLength)
        {
           return Math.Abs(r.NextInt32()) % MaxLength;
        }

        // generate 15 char password from A to Z with each letter being unique
        public static string GeneratePassword()
        {
            var result = "";
            var ch = ' ';
            for (var c=0;c<15;c++)
            {
                var x = 0;
                Counts.Add(3);

                do
                {
                    x = RandVal(26);
                    ch = (char)(x + 65); // A-Z
                } while (result.Contains(ch));
               // Console.Write("{0} ", x);

                result += ch;
            }
            return result;
        }

        // Return index to highest count  
        public static int GetHighestCountIndex()
        {
            var min = 0;
            var minIndex = -1;
            for(var i=0;i< Counts.Count;i++)
            {
                if (Counts[i] > min)
                {
                    min = Counts[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }

        // return index to shortest of five parts
        public static int GetShortestPartIndex(char ch)
        {
            var min = 10;
            var minIndex = -1;
            for (var i = 0; i < partpw.Count; i++)
            {
                if (partpw[i].Length < min && !partpw[i].Contains(ch))
                {
                    min = partpw[i].Length;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        // Scramble each letter in passwords into three and place randomly in three of five strings
        public static void ExpandPassword()
        {
            for (var i = 0; i < 5; i++)
            {
                partpw.Add("");
            }
            var Counter = 45;
            while (Counter > 0)
            {
                var index = GetHighestCountIndex();
 
                if (index >-1)
                {
                    var ch = password[index];
                    var pwIndex = GetShortestPartIndex(ch);
                    if (!partpw[pwIndex].Contains(ch) )
                    {
                        Counts[index]--;
                        partpw[pwIndex]+=ch;
                        Counter--;
                        //Console.WriteLine("{0}", Counter);
                    }
                }
            }
        }

        // Proces Partpw and fill PartPos with indices of positions in password
        private static void GenerateListPos()
        {
            for (var i = 0; i < partpw.Count; i++)
            {
                var aPartpw = partpw[i];
                for (var chindex=0;chindex< aPartpw.Length; chindex++)
                {
                    var lpos = password.IndexOf(aPartpw[chindex]);
                    partpos[i, chindex] = lpos;
                }             
            }
        }

 

        // turns XYZ etc stored in partpw and partpos into xyza-po8y- etc
        private static string Encode(int personIndex)
        {
            var result = "";
            for (var i=0;i<9;i++)
            {
                char ch = partpw[personIndex][i];
                int value = (ch - 'A')*16 + partpos[personIndex,i];
                result += EncInt(value);
                var le = result.Length;
                if (le == 4 || le == 9 || le == 14 || le == 19)
                    result += '-';
            }
            return result;
        }

        // converts 0-35 to A-Z (0-25) or 0-9 (26-35)
        private static char EncByte(int value)
        {
            if (value < 26)
                return (char)(value + 65); // A-Z
            else
                return (char)(value + 22); // subtract 26, add 48 to convert to 0-9
        }

        // Converts int into two base 36 strings
        private static string EncInt(int value)
        {
            var hi = value / 36;
            var li = value % 36;
            return String.Concat(EncByte(hi),EncByte(li));
        }

        // uses testoos and chars to recreate original 15 char password
        private static void Place(string s, int index)
        {

            for (var i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                var chindex = testpos[index, i];
                newpasswordChars[chindex] = ch;
            }
        }

        // Tests any 3 strings (numbered 1-5) returns password
        private static string test(int index1,int index2,int index3)
        {
            var str1 = Decode(Coded[index1 - 1],0);
            var str2 = Decode(Coded[index2 - 1],1);
            var str3 = Decode(Coded[index3 - 1],2);
            Console.WriteLine("Decoding");
            Console.WriteLine(str1);
            Console.WriteLine(str2);
            Console.WriteLine(str3);

            for (var i = 0; i < newpasswordChars.Length; i++)
                newpasswordChars[i] = '*';
            Place(str1,0);
            Place(str2,1);
            Place(str3,2);
            var temppassword = "";
            for (var i = 0; i < newpasswordChars.Length; i++)
                temppassword+=newpasswordChars[i];
            return temppassword;
        }

        // A-Z -> 0-25 '0'..'9' -> 26
        private static int decodeValue(char value)
        {
            if (value >= '0' && value <= '9')
                return value - '0'+26;
            else
                return value-'A';
        }

        // Converts 2 char string to int value using 
        private static int decodePair(string pair)
        {
            return decodeValue(pair[0]) * 36 + decodeValue(pair[1]);
        }

        private static string Decode(string v,int testIndex)
        {
            var result = "";
            var stripped = "";
            foreach(var ch in v) // remove -
            {
                if (ch != '-')
                    stripped += ch;
            }

            var index = 0;
            for (var i = 0; i < stripped.Length; i += 2)
            {
                var pair = stripped.Substring(i, 2);
                var value = decodePair(pair);
                var letter = (char)((value / 16) + 65);
                result += letter;
                var pos = value & 15;
                testpos[testIndex, index++] = pos;
            }
            return result;
        }

        // convert string into memorystream
        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        // Calculate sha-256 of password
        static byte [] GetStringHash(string value)
        {
            var sha256password = SHA256Managed.Create();
            var memstream = GenerateStreamFromString(value);
            return sha256password.ComputeHash(memstream);
        }

        static void Main(string[] args)
        {
            password = GeneratePassword();
            Console.WriteLine(password);

            var hashValue = GetStringHash(password);

            ExpandPassword();
            GenerateListPos();
            for (var i = 0; i < 5; i++)
            {
                Console.WriteLine("{0}", partpw[i]);
                for (var chindex = 0; chindex < partpw[i].Length; chindex++)
                {
                    Console.Write("{0},", partpos[i, chindex]);
                }
                Console.WriteLine();

            }
            for (var i = 0; i < 5; i++)
            {
                Coded.Add(Encode(i));
                Console.WriteLine(Coded[i]);
            }    
            
            var newpassword=test(1,4,5);   
            var newhash = GetStringHash(password);

            if (newhash.SequenceEqual(hashValue)) // Use for comparing two arrays LINQ
            {
                Console.WriteLine("Password Ok");
            }
            else
            {
                Console.WriteLine("Password NOT Ok");
            }
        }

    }

}
