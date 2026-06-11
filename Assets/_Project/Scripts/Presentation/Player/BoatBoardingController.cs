using ChoNoi.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChoNoi.Presentation.Player
{
    public class BoatBoardingController : MonoBehaviour
    {
        [SerializeField] private ShorePlayerController playerController;
        [SerializeField] private Transform playerVisualRoot;
        [SerializeField] private Transform boat;
        [SerializeField] private Transform standPoint;
        [SerializeField] private Transform dismountPoint;
        [SerializeField] private BoatController boatController;
        [SerializeField] private PCBoatInput boatInput;
        [SerializeField] private BoatFollowCamera followCamera;
        [SerializeField] private float interactDistance = 3.2f;
        [SerializeField] private PlayerNpcTradeInteractor npcTradeInteractor;

        private bool isBoarded;

        public bool IsBoarded => isBoarded;

        public void Configure(
            ShorePlayerController controller,
            Transform visualRoot,
            Transform boatTarget,
            Transform stand,
            Transform dismount,
            BoatController controllerBoat,
            PCBoatInput inputBoat,
            BoatFollowCamera cameraRig)
        {
            playerController = controller;
            playerVisualRoot = visualRoot;
            boat = boatTarget;
            standPoint = stand;
            dismountPoint = dismount;
            boatController = controllerBoat;
            boatInput = inputBoat;
            followCamera = cameraRig;
            if (npcTradeInteractor == null)
                npcTradeInteractor = GetComponent<PlayerNpcTradeInteractor>();
            SetBoatControl(false);
        }

        private void Start()
        {
            SetBoatControl(false);
            if (followCamera != null)
                followCamera.Configure(transform);
        }

        private void Update()
        {
            if (Keyboard.current?.eKey.wasPressedThisFrame != true)
                return;

            if (!isBoarded && npcTradeInteractor != null && npcTradeInteractor.TryHandleInteract())
                return;

            if (isBoarded)
                DismountBoat();
            else if (CanBoard())
                BoardBoat();
        }

        private bool CanBoard()
        {
            if (boat == null)
                return false;

            return Vector3.Distance(transform.position, boat.position) <= interactDistance;
        }

        private void BoardBoat()
        {
            isBoarded = true;
            if (playerController != null)
                playerController.CanMove = false;

            if (standPoint != null)
            {
                transform.SetParent(boat, true);
                transform.position = standPoint.position;
                transform.rotation = standPoint.rotation;
            }

            if (playerVisualRoot != null)
                playerVisualRoot.gameObject.SetActive(false);

            SetBoatControl(true);
            if (followCamera != null && boat != null)
                followCamera.Configure(boat);
        }

        private void DismountBoat()
        {
            isBoarded = false;
            transform.SetParent(null, true);

            if (dismountPoint != null)
            {
                transform.position = dismountPoint.position;
                transform.rotation = dismountPoint.rotation;
            }

            if (playerVisualRoot != null)
                playerVisualRoot.gameObject.SetActive(true);

            if (playerController != null)
                playerController.CanMove = true;

            SetBoatControl(false);
            if (followCamera != null)
                followCamera.Configure(transform);
        }

        private void SetBoatControl(bool enabled)
        {
            if (boatController != null)
                boatController.enabled = enabled;
            if (boatInput != null)
                boatInput.enabled = enabled;
        }

        private void OnGUI()
        {
            const int width = 420;
            const int height = 46;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 92f, width, height);

            if (isBoarded)
            {
                GUI.Box(rect, "E: Roi ghe | WASD: Lai ghe | Alt + Mouse: Xoay camera");
            }
            else if (CanBoard())
            {
                GUI.Box(rect, "E: Len ghe | WASD: Di chuyen tren bo");
            }
        }
    }
}
