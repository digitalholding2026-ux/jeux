using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Retour visuel des boutons 3D : la face s'enfonce sur le socle au toucher
    /// puis remonte avec un léger rebond. Joue aussi le clic sonore.
    /// </summary>
    public sealed class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform _face;
        private Vector3 _restPosition;
        private float _depth = 14f;
        private Coroutine _animation;

        public static void Attach(Button button, RectTransform face, float depth)
        {
            if (button == null || face == null)
            {
                return;
            }

            var juicy = button.gameObject.GetComponent<JuicyButton>();
            if (juicy == null)
            {
                juicy = button.gameObject.AddComponent<JuicyButton>();
            }

            juicy._face = face;
            juicy._restPosition = face.localPosition;
            juicy._depth = depth;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Press();
            AudioManager.PlayClick();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Press()
        {
            if (_face == null)
            {
                return;
            }

            StopAnimation();
            _animation = TweenRunner.Run(PressRoutine());
        }

        private void Release()
        {
            if (_face == null)
            {
                return;
            }

            StopAnimation();
            _animation = TweenRunner.Run(ReleaseRoutine());
        }

        private System.Collections.IEnumerator PressRoutine()
        {
            var from = _face.localPosition;
            var to = _restPosition + Vector3.down * (_depth * 0.75f);
            var fromScale = _face.localScale;
            var toScale = Vector3.one * 0.97f;

            var elapsed = 0f;
            const float duration = 0.07f;
            while (elapsed < duration)
            {
                if (_face == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Ease.OutQuad(Mathf.Clamp01(elapsed / duration));
                _face.localPosition = Vector3.LerpUnclamped(from, to, t);
                _face.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                yield return null;
            }
        }

        private System.Collections.IEnumerator ReleaseRoutine()
        {
            var from = _face.localPosition;
            var to = _restPosition;
            var fromScale = _face.localScale;
            var toScale = Vector3.one;

            var elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                if (_face == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Ease.OutBack(Mathf.Clamp01(elapsed / duration));
                _face.localPosition = Vector3.LerpUnclamped(from, to, t);
                _face.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                yield return null;
            }

            if (_face != null)
            {
                _face.localPosition = _restPosition;
                _face.localScale = Vector3.one;
            }
        }

        private void StopAnimation()
        {
            if (_animation != null)
            {
                TweenRunner.Stop(_animation);
                _animation = null;
            }
        }
    }
}
