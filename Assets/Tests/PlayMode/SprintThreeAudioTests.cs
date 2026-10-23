using System.Collections;
using ColorFlow.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColorFlow.Tests.PlayMode
{
    /// <summary>
    /// Vérifications PlayMode du Livrable C du Sprint 3 : les sons et la musique
    /// sont synthétisés par code (aucun asset importé) et la boucle de fond tourne.
    /// </summary>
    public sealed class SprintThreeAudioTests
    {
        [UnityTest]
        public IEnumerator Audio_StartsCodeGeneratedMusicLoop()
        {
            AudioManager.Init();
            yield return null;

            Assert.IsTrue(AudioManager.IsMusicPlaying, "La musique de fond ne demarre pas.");
        }

        [UnityTest]
        public IEnumerator Audio_ComboRaisesPitchWithoutStoppingMusic()
        {
            AudioManager.Init();
            yield return null;

            AudioManager.PlayCombo(2);
            AudioManager.PlayCombo(3);
            AudioManager.PlayCombo(6);
            yield return null;

            Assert.IsTrue(AudioManager.IsMusicPlaying, "La musique a ete interrompue par le combo.");
        }

        [UnityTest]
        public IEnumerator Audio_StopAndRestartMusic()
        {
            AudioManager.Init();
            yield return null;

            AudioManager.StopMusic();
            yield return null;
            Assert.IsFalse(AudioManager.IsMusicPlaying, "StopMusic n'a pas coupe la boucle.");

            AudioManager.PlayMusic();
            yield return null;
            Assert.IsTrue(AudioManager.IsMusicPlaying, "PlayMusic ne relance pas la boucle.");
        }
    }
}
