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
        paddlePosY = 112,
        ballPosX = 113,
        ballPosY = 114,
        leftSideScore = 115,
        rightSideScore = 116
    }

    // Переделать в статический класс возврощающий словарь или ещё что-то
    class NetworkData
    {
        public Dictionary<char, int> dataDictionary = new Dictionary<char, int>();

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
                            dataDictionary[key] = int.Parse(partMessage.ToString());
                        }
                        else
                        {
                            dataDictionary.Add(key, int.Parse(partMessage.ToString()));
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
                        dataDictionary[key] = int.Parse(partMessage.ToString());
                    }
                    else
                    {
                        dataDictionary.Add(key, int.Parse(partMessage.ToString()));
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
