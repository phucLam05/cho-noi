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
<<<<<<< HEAD
=======
        [SerializeField] private float targetRefreshInterval = 0.15f;
>>>>>>> origin/Animation

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

<<<<<<< HEAD
            // Dynamically add NpcCustomerBehavior to all NPC trade targets in the scene (excluding Upgrade targets)
            NpcTradeTarget[] targets = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target.TargetType != InteractionTargetType.Upgrade)
                {
                    if (target.GetComponent<NpcCustomerBehavior>() == null)
                    {
                        target.gameObject.AddComponent<NpcCustomerBehavior>();
                    }
                }
            }
        }

        private bool CheckIsAnyUIOpen(FullSimulatorUI fullUI)
        {
            if (fullUI != null)
            {
                if (fullUI.IsDialogueOpen || fullUI.IsMarketingOpen || fullUI.IsPauseOpen || 
                    fullUI.IsSettingsOpen || fullUI.IsTutorialOpen || fullUI.IsYardOpen || 
                    fullUI.IsTradeQtyOpen)
                {
                    return true;
                }
            }
            if (hud != null && (hud.IsUpgradeOpen || hud.IsNpcTradeOpen))
            {
                return true;
            }
            if (bargainingUI != null && !bargainingUI.IsHidden)
            {
                return true;
            }
            return false;
=======
            fullUI = FindAnyObjectByType<FullSimulatorUI>();
            boardingController = GetComponent<BoatBoardingController>();
            RefreshTargets();
>>>>>>> origin/Animation
        }

        private void Update()
        {
<<<<<<< HEAD
            var fullUI = FindAnyObjectByType<FullSimulatorUI>();
            bool isAnyUIOpen = CheckIsAnyUIOpen(fullUI);
=======
            bool isDialogueOpen = fullUI != null && fullUI.IsDialogueOpen;
            bool isUpgradeOpen = hud != null && hud.IsUpgradeOpen;

            // When dialogue or upgrade UI is active, the player is interacting (inputs frozen elsewhere)
            bool isAnyUIOpen = isDialogueOpen || isUpgradeOpen || (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);
>>>>>>> origin/Animation

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

<<<<<<< HEAD
            var fullUI = FindAnyObjectByType<FullSimulatorUI>();
            bool isAnyUIOpen = CheckIsAnyUIOpen(fullUI);
=======
            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();

            bool isDialogueOpen = fullUI != null && fullUI.IsDialogueOpen;
            bool isUpgradeOpen = hud != null && hud.IsUpgradeOpen;
            bool isAnyUIOpen = isDialogueOpen || isUpgradeOpen || (hud != null && hud.IsNpcTradeOpen) || (bargainingUI != null && !bargainingUI.IsHidden);
>>>>>>> origin/Animation

            if (isAnyUIOpen)
                CloseTrade();
            else
                OpenTrade(currentTarget);

            return true;
        }

        public NpcTradeTarget ActiveTradeTarget => activeTradeTarget;

        public void OpenTrade(NpcTradeTarget target)
        {
            if (target == null)
                return;

            activeTradeTarget = target;
            SetNpcPaused(activeTradeTarget, true);

            // Freeze player controller movement
            if (playerController != null)
                playerController.CanMove = false;

            // Freeze boat controls if on boat
<<<<<<< HEAD
            var boarding = GetComponent<BoatBoardingController>();
            if (boarding != null && boarding.IsBoarded)
            {
                boarding.SetBoatControlActive(false);
            }

            var fullUI = FindAnyObjectByType<FullSimulatorUI>();
=======
            if (boardingController == null)
                boardingController = GetComponent<BoatBoardingController>();

            if (boardingController != null && boardingController.IsBoarded)
            {
                boardingController.SetBoatControlActive(false);
            }

            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();
>>>>>>> origin/Animation

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

<<<<<<< HEAD
            var boarding = GetComponent<BoatBoardingController>();
            bool onBoat = boarding != null && boarding.IsBoarded;
=======
            if (boardingController == null)
                boardingController = GetComponent<BoatBoardingController>();

            bool onBoat = boardingController != null && boardingController.IsBoarded;
>>>>>>> origin/Animation

            // Unfreeze player movement only if NOT on the boat
            if (playerController != null)
                playerController.CanMove = !onBoat;

            // Unfreeze boat controls if on the boat
<<<<<<< HEAD
            if (boarding != null && onBoat)
            {
                boarding.SetBoatControlActive(true);
=======
            if (boardingController != null && onBoat)
            {
                boardingController.SetBoatControlActive(true);
>>>>>>> origin/Animation
            }

            if (hud != null)
                hud.CloseAllPanels();
                
            if (bargainingUI != null)
                bargainingUI.ToggleVisibility(false);

<<<<<<< HEAD
            var fullUI = FindAnyObjectByType<FullSimulatorUI>();
=======
            if (fullUI == null)
                fullUI = FindAnyObjectByType<FullSimulatorUI>();

>>>>>>> origin/Animation
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

<<<<<<< HEAD
=======
        private void RefreshTargets()
        {
            cachedTargets = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
        }

>>>>>>> origin/Animation
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
