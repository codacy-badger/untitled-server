using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace UntitledSandbox_Server
{
    [System.Serializable]
    public class GameObject
    {
        public string prefabPath { get; set; }
        public float ID { get; set; }
        public Vector3 pos { get; set; }
        public Vector3 rot { get; set; }
    }

    public class ObjectsHelper
    {
        public static string EncodeGameObject(GameObject obj)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0},{1},{2},{3},{4},{5},{6}", obj.prefabPath, obj.pos.x, obj.pos.y, obj.pos.z, obj.rot.x, obj.rot.y, obj.rot.z);
            byte[] bytes = Encoding.ASCII.GetBytes(builder.ToString());
            return Convert.ToBase64String(bytes);
        }

        public static GameObject DecodeGameObject(string obj)
        {
            obj = Encoding.ASCII.GetString(Convert.FromBase64String(obj));
            GameObject gameObj = new GameObject();
            string[] data = obj.Split(',');
            gameObj.prefabPath = data[0];
            gameObj.pos.x = float.Parse(data[0]);
            gameObj.pos.y = float.Parse(data[0]);
            gameObj.pos.z = float.Parse(data[0]);
            gameObj.rot.x = float.Parse(data[0]);
            gameObj.rot.y = float.Parse(data[0]);
            gameObj.rot.z = float.Parse(data[0]);
            return gameObj;
        }
    }

}
