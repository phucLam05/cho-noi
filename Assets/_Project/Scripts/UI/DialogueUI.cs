using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image npcPortrait;
        [SerializeField] private Image playerPortrait;
        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private RectTransform choiceContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Configuration")]
        [SerializeField] private float charactersPerSecond = 30f;

        private List<GameObject> activeChoiceButtons = new List<GameObject>();
        private Tween typewriterTween;

        public Image NpcPortrait => npcPortrait;
        public Image PlayerPortrait => playerPortrait;
        public TMP_Text NpcNameText => npcNameText;
        public TMP_Text DialogueText => dialogueText;
        public RectTransform ChoiceContainer => choiceContainer;

        public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

        public void Open()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                // Scale in animation
                dialoguePanel.transform.localScale = new Vector3(1f, 0f, 1f);
                dialoguePanel.transform.DOScaleY(1f, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        public void Close()
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                dialoguePanel.transform.DOScaleY(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    dialoguePanel.SetActive(false);
                    ClearChoices();
                });
            }
        }

        public void SetNPCName(string name)
        {
            if (npcNameText != null)
            {
                npcNameText.text = name;
            }
        }

        public void SetDialogueText(string text, bool typewriter = true)
        {
            if (dialogueText == null) return;

            if (typewriterTween != null)
            {
                typewriterTween.Kill();
            }

            if (typewriter)
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = 0;
                float duration = text.Length / charactersPerSecond;
                typewriterTween = DOTween.To(
                    () => dialogueText.maxVisibleCharacters,
                    x => dialogueText.maxVisibleCharacters = x,
                    text.Length,
                    duration
                ).SetEase(Ease.Linear);
            }
            else
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = text.Length;
            }
        }

        public void SetPortraits(Sprite npcSprite, Sprite playerSprite)
        {
            if (npcPortrait != null)
            {
                if (npcSprite != null)
                {
                    npcPortrait.gameObject.SetActive(true);
                    npcPortrait.sprite = npcSprite;
                    npcPortrait.color = Color.white;
                }
                else
                {
                    npcPortrait.gameObject.SetActive(false);
                }
            }

            if (playerPortrait != null)
            {
                if (playerSprite != null)
                {
                    playerPortrait.gameObject.SetActive(true);
                    playerPortrait.sprite = playerSprite;
                    playerPortrait.color = Color.white;
                }
                else
                {
                    playerPortrait.gameObject.SetActive(false);
                }
            }
        }

        public void ClearChoices()
        {
            foreach (var btn in activeChoiceButtons)
            {
                if (btn != null) Destroy(btn);
            }
            activeChoiceButtons.Clear();
            if (choiceContainer != null)
            {
                choiceContainer.gameObject.SetActive(false);
            }
        }

        public void ShowChoices()
        {
            if (choiceContainer != null && activeChoiceButtons.Count > 0)
            {
                choiceContainer.gameObject.SetActive(true);
                // Slide in choice container or scale in
                choiceContainer.localScale = Vector3.zero;
                choiceContainer.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }
        }

        public void AddChoice(string choiceText, UnityEngine.Events.UnityAction onClick)
        {
            if (choiceContainer == null || choiceButtonPrefab == null) return;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            btnObj.name = $"Choice_{activeChoiceButtons.Count + 1}";

            // Find elements
            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = choiceText;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(onClick);
                
                // Add hover and click scale transitions
                btn.transform.localScale = Vector3.one;
                
                // Set up event triggers or simple hover script for button scale
                var hoverEffect = btnObj.AddComponent<UIHoverScaleEffect>();
                hoverEffect.hoverScale = 1.05f;
            }

            activeChoiceButtons.Add(btnObj);
        }
    }

    // Helper script for hover scale effects
    public class UIHoverScaleEffect : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public float hoverScale = 1.05f;
        public float duration = 0.15f;
        private Vector3 originalScale = Vector3.one;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutSine);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.DOScale(originalScale, duration).SetEase(Ease.OutSine);
        }
    }
}
