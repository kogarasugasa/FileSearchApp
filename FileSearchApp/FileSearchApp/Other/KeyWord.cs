using System;
using CustomFunctions;

namespace FileSearchApp
{
    public enum MatchToExtension { Exclude, Include }
    public sealed class KeyWord : IEquatable<KeyWord>
    {
        public string Word = string.Empty;
        public bool Fuzzy = false;
        // public KeyWord(string word, bool fuzzy)
        // {
        //     this.Word = word;
        //     this.Fuzzy = fuzzy;
        // }
        public bool MatchTo(ExFileInfo pInfo, MatchToExtension pIncludeExtension)
        {
            if (pInfo.IsDefault()){
                return false;
            }
            if (this.Fuzzy){
                if (pIncludeExtension == MatchToExtension.Include){
                    return MyTool.FuzzyContains(this.Word, pInfo.FileName + pInfo.Extension);
                }
                else{
                    return MyTool.FuzzyContains(this.Word, pInfo.FileName);
                }
            }
            else{
                if (pIncludeExtension == MatchToExtension.Include){
                    return this.Word.ToLower() == (pInfo.FileName + pInfo.Extension).ToLower();
                }
                else{
                    return this.Word.ToLower() == pInfo.FileName.ToLower();
                }
            }
        }
        public KeyWord Clone()
        {
            return new KeyWord { Word = this.Word, Fuzzy = this.Fuzzy };
        }
        public override bool Equals(object obj)
        {
            return obj != null &&
                obj.GetType() == this.GetType() &&
                this.Equals((KeyWord)obj)
            ;
        }
        public bool Equals(KeyWord other)
        {
            return other != null &&
                other.Word == this.Word &&
                other.Fuzzy == this.Fuzzy
            ;
        }
        public override int GetHashCode()
        {
            return (Word.GetHashCode() * 3) ^ Fuzzy.GetHashCode();
        }
    }
}
