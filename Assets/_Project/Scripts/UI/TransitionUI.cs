using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class TransitionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject transitionPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        private void Start()
        {
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(false);
                if (canvasGroup == null)
                {
                    canvasGroup = transitionPanel.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = transitionPanel.AddComponent<CanvasGroup>();
                    }
                }
            }
        }

        public void FadeIn(float duration, System.Action onComplete = null)
        {
            if (transitionPanel == null || canvasGroup == null) return;

            transitionPanel.SetActive(true);
            canvasGroup.alpha = 1f;

            canvasGroup.DOFade(0f, duration).SetEase(Ease.OutSine).SetUpdate(true).OnComplete(() =>
            {
                transitionPanel.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void FadeOut(float duration, System.Action onComplete = null)
        {
            if (transitionPanel == null || canvasGroup == null) return;

            transitionPanel.SetActive(true);
            canvasGroup.alpha = 0f;

            canvasGroup.DOFade(1f, duration).SetEase(Ease.InSine).SetUpdate(true).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }
    }
}
