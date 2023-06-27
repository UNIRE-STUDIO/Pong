using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    public enum Keys
    {
        b1 = 1,
        b2 = 2,
        b3 = 3,
        b4 = 4,
        b5 = 5,
        b6 = 6,
        b7 = 7,
        b8 = 8,
        b9 = 9,
        b10 = 10,
        b11 = 11,
        b12 = 12,
        b13 = 13,
        b14 = 14,
        b15 = 15,
        b16 = 16,
        b17 = 17,
        b18 = 18,
        b19 = 19,
        b20 = 20,
        paddlePosY = 112,
        leftSideScore = 115,
        rightSideScore = 116
    }

    // Переделать в статический класс возврощающий словарь или ещё что-то
    class NetworkData
    {
        public Dictionary<char, string> dataDictionary = new Dictionary<char, string>();

        private int lengthKeys = typeof(Keys).GetFields().Length;

        StringBuilder dataBuffer = new StringBuilder();

        public override string ToString()
        {
            dataBuffer.Clear();
            foreach (var item in dataDictionary.Keys)
            {
                dataBuffer.Append($"{item}{dataDictionary[item]}");
            }
            return dataBuffer.ToString();
        }

        public void Unpacking(string message)
        {
            if (message == null)
            {
                throw new Exception("message == null");
            }
            char key = message[0];
            StringBuilder partMessage = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                bool checkKey = false;
                foreach(var a in Enum.GetValues(typeof(Keys)))
                {
                    if (message[i] == (char)(Keys)a)
                    {
                        checkKey = true;
                        break;
                    }
                }
                if (checkKey)
                {
                    if (i != 0)
                    {
                        if (dataDictionary.ContainsKey(key))
                        {
                            dataDictionary[key] = partMessage.ToString();
                        }
                        else
                        {
                            dataDictionary.Add(key, partMessage.ToString());
                        }
                        partMessage.Clear();
                    }
                    key = message[i];
                }
                else if (i == message.Length - 1) 
                {
                    partMessage.Append(message[i]);
                    if (dataDictionary.ContainsKey(key))
                    {
                        dataDictionary[key] = partMessage.ToString();
                    }
                    else
                    {
                        dataDictionary.Add(key, partMessage.ToString());
                    }
                }
                else
                {
                    partMessage.Append(message[i]);
                }
            }
        }
    }
}
