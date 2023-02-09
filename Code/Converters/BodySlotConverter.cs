using Code.OutfitPatcher.Armor;
using Mutagen.Bethesda.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.OutfitPatcher.Converters
{
    public class BodySlotConverter : JsonConverter<TBodySlot>
    {
        public override TBodySlot ReadJson(JsonReader reader, Type objectType, TBodySlot existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, TBodySlot value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
