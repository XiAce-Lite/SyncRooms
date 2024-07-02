using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SyncRooms.ViewModel
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Room>? Rooms { get; set; }
       
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        public MainWindowViewModel()
        {
            Rooms = [];
        }

        public class RoomsRoot
        {
            public List<Room>? Rooms { get; set; } = [];

            public void Add(Room item)
            {
                Rooms ??= [];
                if (item is null) { return; }
                Rooms.Add(item);
            }
        }

        public class Room
        {
            private List<string>? _tags = [];

            [JsonPropertyName("roomId")]
            public string RoomId { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("roomPurpose")]
            public string RoomPurpose { get; set; } = string.Empty;

            [JsonPropertyName("roomPublishType")]
            public string RoomPublishType { get; set; } = string.Empty;

            [JsonPropertyName("needPasswd")]
            public bool NeedPasswd { get; set; } = false;

            [JsonPropertyName("roomStatus")]
            public string RoomStatus { get; set; } = string.Empty;

            [JsonPropertyName("tags")]
            public List<string>? Tags {
                get {
                    if ((_tags is not null) && (CustomTags is not null))
                    {
                        List<string> _tags2 = [.. _tags, .. CustomTags];
                        return _tags2;
                    }
                    return _tags;
                }
                set {
                    _tags = value;
                }
            }

            [JsonPropertyName("customTags")]
            public List<string>? CustomTags { get; set; } = [];

            [JsonPropertyName("isTestRoom")]
            public bool IsTestRoom { get; set; } = false;

            [JsonPropertyName("ownerUser")]
            public OwnerUser? OwnerUser { get; set; }

            [JsonPropertyName("members")]
            public List<Member>? Members { get; set; } = [];
        }

        public class Member : OwnerUser
        {
            [JsonPropertyName("roomEnterType")]
            public string RoomEnterType { get; set; } = string.Empty;
        }

        public class Avatar
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("preset")]
            public Preset? Preset { get; set; }

            private string _url = "";

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("url")]
            public string Url
            {
                get
                {
                    if (string.IsNullOrEmpty(Type)) { return ""; }
                    if (Type == "preset")
                    {
                        if (!string.IsNullOrEmpty(Preset?.ShapeKey) || !string.IsNullOrEmpty(Preset?.ColorCode))
                        {
                            _url = $"https://syncroom.yamaha.com/assets-v2/img/play/room/preset/avatar_{Preset.ShapeKey}_{Preset.ColorCode}.png";
                            return _url;
                        }
                    }
                    return _url;
                }
                set
                {
                    _url = value;
                }
            }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("special")]
            public Special? Special { get; set; }
        }

        public class Preset
        {
            [JsonPropertyName("colorCode")]
            public string ColorCode { get; set; } = string.Empty;

            [JsonPropertyName("shapeKey")]
            public string ShapeKey { get; set; } = string.Empty;
        }

        public class Special
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("key")]
            public string Key { get; set; } = string.Empty;
        }

        public class LastPlayedPart
        {
            private string _lastPart = "";

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("part")]
            public string Part { 
                get
                {
                    if (_lastPart == "custom") { 
                        _lastPart = CustomPart;
                    }
                    return _lastPart;
                }
                set { 
                    _lastPart = value;
                } 
            }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("customPart")]
            public string CustomPart { get; set; } = string.Empty;
        }

        public class OwnerUser
        {
            [JsonPropertyName("userId")]
            public string UserId { get; set; } = string.Empty;

            [JsonPropertyName("nickname")]
            public string Nickname { get; set; } = string.Empty;

            [JsonPropertyName("idProvider")]
            public string IdProvider { get; set; } = string.Empty;

            [JsonPropertyName("avatar")]
            public Avatar? Avatar { get; set; }

            [JsonPropertyName("isBeginner")]
            public bool IsBeginner { get; set; }

            [JsonPropertyName("lastPlayedPart")]
            public LastPlayedPart? LastPlayedPart { get; set; }
        }
    }
}
