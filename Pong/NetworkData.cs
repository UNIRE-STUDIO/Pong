using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    class NetworkData
    {
        public Dictionary<char, int> dataDictionary = new Dictionary<char, int>();

        // Стоит вынести отдельно
        public char[] keys = {(char)112, (char)113, (char)114};

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
            char key = message[0];
            StringBuilder partMessage = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                bool checkKey = false;
                for (int j = 0; j < keys.Length; j++)
                {
                    if (message[i] == keys[j])
                    {
                        checkKey = true;
                        break;
                    }
                }
                if (checkKey)
                {
                    if (i != 0)
                    {
                        dataDictionary.Add(key, int.Parse(partMessage.ToString()));
                        partMessage.Clear();
                    }
                    key = message[i];
                }
                else if (i == message.Length - 1) 
                {
                    partMessage.Append(message[i]);
                    dataDictionary.Add(key, int.Parse(partMessage.ToString()));
                }
                else
                {
                    partMessage.Append(message[i]);
                }
            }
        }
    }
}
