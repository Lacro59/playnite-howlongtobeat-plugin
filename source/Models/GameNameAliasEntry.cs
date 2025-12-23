using CommonPluginsShared;
using System;

namespace HowLongToBeat.Models
{
    public class GameNameAliasEntry : ObservableObjectPlus
    {
        private string _source;
        public string Source
        {
            get => _source;
            set => SetValue(ref _source, value);
        }

        private string _target;
        public string Target
        {
            get => _target;
            set => SetValue(ref _target, value);
        }

        public GameNameAliasEntry()
        {
        }

        public GameNameAliasEntry(string source, string target)
        {
            Source = source;
            Target = target;
        }

        public bool IsValid => !string.IsNullOrEmpty(Source) && !string.IsNullOrEmpty(Target);

        public override string ToString()
        {
            return $"{Source} -> {Target}";
        }
    }
}
