using ChoNoi.Presentation.NPC;
using ChoNoiMienTay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChoNoi.Presentation.Player
{
    public class PlayerNpcTradeInteractor : MonoBehaviour
    {
        [SerializeField] private RiverMarketHUD hud;
        [SerializeField] private ShorePlayerController playerController;
        [SerializeField] private BargainingPrototypeUI bargainingUI;

        private NpcTradeTarget currentTarget;
        private NpcTradeTarget activeTradeTarget;

        public bool HasTradeTargetInRange => currentTarget != null;

        private void Start()
        {
            if (hud == null)
                hud = FindAnyObjectByType<RiverMarketHUD>();

            if (playerController == null)
                playerController = GetComponent<ShorePlayerController>();

            if (bargainingUI == null)
                bargainingUI = FindAnyObjectByType<BargainingPrototypeUI>();
        }

        private void Update()
        {
            if (playerController != null && !playerController.CanMove)
                return;

            currentTarget = FindClosestTarget();

            bool isAnyUIOpen = (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);

            if (isAnyUIOpen && (currentTarget == null || Keyboard.current?.escapeKey.wasPressedThisFrame == true))
                CloseTrade();
        }

        public bool TryHandleInteract()
        {
            if (currentTarget == null)
                return false;

            bool isAnyUIOpen = (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);

            if (isAnyUIOpen)
                CloseTrade();
            else
                OpenTrade(currentTarget);

            return true;
        }

        private void OpenTrade(NpcTradeTarget target)
        {
            if (target == null)
                return;

            activeTradeTarget = target;
            SetNpcPaused(activeTradeTarget, true);

            switch (target.TargetType)
            {
                case InteractionTargetType.Bargain:
                    if (bargainingUI != null) bargainingUI.ToggleVisibility(true);
                    break;
                case InteractionTargetType.Upgrade:
                    if (hud != null) hud.OpenUpgradePanel();
                    break;
                case InteractionTargetType.News:
                    if (hud != null) hud.OpenNewsPanel();
                    break;
                case InteractionTargetType.Trade:
                    if (hud != null) hud.OpenNpcTrade(activeTradeTarget.NpcDisplayName);
                    break;
            }
        }

        private void CloseTrade()
        {
            SetNpcPaused(activeTradeTarget, false);
            activeTradeTarget = null;

            if (hud != null)
                hud.CloseNpcTrade();
                
            if (bargainingUI != null)
                bargainingUI.ToggleVisibility(false);
        }

        private NpcTradeTarget FindClosestTarget()
        {
            NpcTradeTarget[] targets = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
            NpcTradeTarget closest = null;
            float closestDistance = float.MaxValue;

            foreach (NpcTradeTarget target in targets)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > target.InteractionRadius || distance >= closestDistance)
                    continue;

                closest = target;
                closestDistance = distance;
            }

            return closest;
        }

        private void OnGUI()
        {
            if (currentTarget == null)
                return;

            Rect rect = new Rect((Screen.width - 420f) * 0.5f, Screen.height - 146f, 420f, 42f);
            GUI.Box(rect, $"E: Giao dich voi {currentTarget.NpcDisplayName}");
        }

        private static void SetNpcPaused(NpcTradeTarget target, bool paused)
        {
            if (target == null)
                return;

            SimpleNpcWander wander = target.GetComponent<SimpleNpcWander>();
            if (wander != null)
                wander.SetPaused(paused);
        }
    }
}
