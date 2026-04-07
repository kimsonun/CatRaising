using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatRaising.Core
{
    /// <summary>
    /// Simple screen fade utility using a full-screen black Image.
    /// 
    /// SETUP: Create a full-screen Image under Canvas (color=black, raycastTarget=false),
    /// start with alpha=0. Assign to this script.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.3f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (fadeImage != null)
            {
                var c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.raycastTarget = false;
                fadeImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Fade to black, run action, fade back in.
        /// </summary>
        public void FadeAndExecute(Action duringBlack)
        {
            StartCoroutine(FadeRoutine(duringBlack));
        }

        private IEnumerator FadeRoutine(Action duringBlack)
        {
            if (fadeImage != null)
                fadeImage.raycastTarget = true; // Block input during fade

            // Fade out (to black)
            yield return StartCoroutine(FadeTo(1f));

            // Execute action during black screen
            duringBlack?.Invoke();

            // Small pause while black
            yield return new WaitForSeconds(0.1f);

            // Fade in (from black)
            yield return StartCoroutine(FadeTo(0f));

            if (fadeImage != null)
                fadeImage.raycastTarget = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeImage == null) yield break;

            float startAlpha = fadeImage.color.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                var c = fadeImage.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                fadeImage.color = c;
                yield return null;
            }

            var final_c = fadeImage.color;
            final_c.a = targetAlpha;
            fadeImage.color = final_c;
        }
    }
}
