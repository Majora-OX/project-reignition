using Godot;
using Godot.Collections;

namespace Project.Gameplay.Triggers
{
    public class DialogTrigger : StageTriggerModule
    {
        public override void Activate() => SoundManager.instance.PlayDialog(this);

        public int DialogCount => textKeys.Count;

        [Export]
        public Array<string> textKeys;
        [Export]
        public Array<AudioStream> englishVoiceClips;
        [Export]
        public Array<AudioStream> japaneseVoiceClips;

        public bool IsInvalid()
		{
            if (textKeys.Count != englishVoiceClips.Count || textKeys.Count != japaneseVoiceClips.Count)
			{
                GD.PrintErr($"Dialog trigger {Name} isn't configured properly and cannot be played.");
                return true;
			}

            return false;
		}
    }
}
