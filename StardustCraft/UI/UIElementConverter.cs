

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace StardustCraft.UI
{
    public class UIElementConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        => objectType == typeof(UIElement);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            string type = obj["type"].Value<string>();

            UIElement el = type switch
            {
                "Container" => new UIContainer(),
                "Text" => new UIText(),
                _ => throw new Exception($"Unknown UI type {type}")
            };
            serializer.Populate(obj.CreateReader(), el);
            return el;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }

}
