using ChoNoi.Presentation.NPC;
using ChoNoiMienTay.UI;
using ChoNoi.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChoNoi.Presentation.Player
{
    public class PlayerNpcTradeInteractor : MonoBehaviour
    {
        [SerializeField] private RiverMarketHUD hud;
        [SerializeField] private ShorePlayerController playerController;
        [SerializeField] private BargainingPrototypeUI bargainingUI;
        [SerializeField] private float targetRefreshInterval = 0.15f;

        private NpcTradeTarget currentTarget;
        private NpcTradeTarget activeTradeTarget;
        private NpcTradeTarget[] cachedTargets;
        private FullSimulatorUI fullUI;
        private BoatBoardingController boardingController;
        private float nextTargetRefreshTime;

        public bool HasTradeTargetInRange => currentTarget != null;
        public NpcTradeTarget CurrentTarget => currentTarget;

        private void Start()
        {
            if (hud == null)
                hud = FindAnyObjectByType<RiverMarketHUD>();

            if (playerController == null)
                playerController = GetComponent<ShorePlayerController>();

            if (bargainingUI == null)
                bargainingUI = FindAnyObjectByType<BargainingPrototypeUI>();

            fullUI = FindAnyObjectByType<FullSimulatorUI>();
            boardingController = GetComponent<BoatBoardingController>();
            RefreshTargets();
        }

        private void Update()
        {
            bool isDialogueOpen = fullUI != null && fullUI.IsDialogueOpen;
            bool isUpgradeOpen = hud != null && hud.IsUpgradeOpen;

            // When dialogue or upgrade UI is active, the player is interacting (inputs frozen elsewhere)
            bool isAnyUIOpen = isDialogueOpen || isUpgradeOpen || (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);

            if (playerController != null && !playerController.CanMove && !isAnyUIOpen)
            {
                // Safety release if movement was somehow locked but no UI is active
                playerController.CanMove = true;
            }

            if (playerController != null && !playerController.CanMove && isAnyUIOpen)
            {
                // If UI is open, check if we need to close it due to escape key
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                {
                    CloseTrade();
                }
                return;
            }

            if (Time.time >= nextTargetRefreshTime)
            {
                nextTargetRefreshTime = Time.time + targetRefreshInterval;
                currentTarget = FindClosestTarget();
            }

            if (isAnyUIOpen && currentTarget == null)
                CloseTrade();
        }

        public bool TryHandleInteract()
        {
            if (currentTarget == null)
                return false;

            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();

            bool isDialogueOpen = fullUI != null && fullUI.IsDialogueOpen;
            bool isUpgradeOpen = hud != null && hud.IsUpgradeOpen;
            bool isAnyUIOpen = isDialogueOpen || isUpgradeOpen || (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);

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

            // Freeze player controller movement
            if (playerController != null)
                playerController.CanMove = false;

            // Freeze boat controls if on boat
            if (boardingController == null)
                boardingController = GetComponent<BoatBoardingController>();

            if (boardingController != null && boardingController.IsBoarded)
            {
                boardingController.SetBoatControlActive(false);
            }

            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();

            switch (target.TargetType)
            {
                case InteractionTargetType.Bargain:
                    if (fullUI != null)
                        fullUI.OpenBargainDialogue(target);
                    else if (bargainingUI != null)
                        bargainingUI.ToggleVisibility(true);
                    break;
                case InteractionTargetType.Upgrade:
                    if (fullUI != null)
                        fullUI.OpenUpgradeCampDialogue(target);
                    else if (hud != null)
                        hud.OpenUpgradePanel();
                    break;
                case InteractionTargetType.News:
                case InteractionTargetType.Trade:
                    if (fullUI != null)
                        fullUI.OpenTradeDialogue(target);
                    else if (hud != null)
                        hud.OpenNpcTrade(activeTradeTarget.NpcDisplayName);
                    break;
            }
        }

        public void CloseTrade()
        {
            SetNpcPaused(activeTradeTarget, false);
            activeTradeTarget = null;

            if (boardingController == null)
                boardingController = GetComponent<BoatBoardingController>();

            bool onBoat = boardingController != null && boardingController.IsBoarded;

            // Unfreeze player movement only if NOT on the boat
            if (playerController != null)
                playerController.CanMove = !onBoat;

            // Unfreeze boat controls if on the boat
            if (boardingController != null && onBoat)
            {
                boardingController.SetBoatControlActive(true);
            }

            if (hud != null)
                hud.CloseAllPanels();
                
            if (bargainingUI != null)
                bargainingUI.ToggleVisibility(false);

            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();

            if (fullUI != null)
                fullUI.CloseAllDialogueAndPanels();
        }

        private NpcTradeTarget FindClosestTarget()
        {
            if (cachedTargets == null || cachedTargets.Length == 0)
                RefreshTargets();

            NpcTradeTarget closest = null;
            float closestDistance = float.MaxValue;

            foreach (NpcTradeTarget target in cachedTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                    continue;

                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > target.InteractionRadius || distance >= closestDistance)
                    continue;

                closest = target;
                closestDistance = distance;
            }

            return closest;
        }

        private void RefreshTargets()
        {
            cachedTargets = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
        }

        // Commented out OnGUI to avoid overlap with Canvas side prompts
        /*
        private void OnGUI()
        {
            if (currentTarget == null)
                return;

            Rect rect = new Rect((Screen.width - 420f) * 0.5f, Screen.height - 146f, 420f, 42f);
            GUI.Box(rect, $"E: Giao dich voi {currentTarget.NpcDisplayName}");
        }
        */

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
