using System.Globalization;
using System.Windows.Data;

namespace SyncRooms.Converters
{
    class TagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) { return string.Empty; }

            if (value is not string) { return string.Empty; }

            return value switch
            {
                "testing" => "テスト中",
                "chatting" => "おしゃべり",
                "practicing" => "練習中",
                "streaming" => "配信中",
                "beginnersWelcome" => "初心者歓迎",
                "rock" => "Rock",
                "hardRockHardMetal" => "HR/HM",
                "jPop" => "J-Pop",
                "anime" => "アニメ",
                "idol" => "アイドル",
                "seekingMembers" => "メンバー募集",
                "noVoiceWelcome" => "無言OK",
                "seekingVocalists" => "ボーカル募集",
                "seekingKeyboardist" => "キーボード募集",
                "seekingGuitarists" => "ギタリスト募集",
                "seekingBassist" => "ベーシスト募集",
                "seekingDrummers" => "ドラム募集",
                "seekingAccompaniment" => "伴奏募集",
                "eventInProgress" => "イベント開催中",
                "classic" => "Classic",
                "countryFolk" => "Country/Folk",
                "clubMusicEdm" => "イベント開催中",
                "hipHopRap" => "HipHop/Rap",
                "rnbSoul" => "R&B/Soul",
                "jazz" => "Jazz",
                "fusion" => "Fusion",
                "pop" => "洋楽",
                "world" => "World",
                "bocchiTheRock" => "ぼっち・ざ・ろっく！",
                "vocaloid" => "ボカロ",
                "recording" => "録音中",
                "kPop" => "K-Pop",
                "games" => "ゲーム",
                _ => value,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
