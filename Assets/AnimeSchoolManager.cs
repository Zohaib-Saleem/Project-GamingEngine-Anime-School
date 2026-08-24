using KikiNgao.SimpleBikeControl;
using SickscoreGames.HUDNavigationSystem;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class AnimeSchoolManager : MonoBehaviour
{
    public GameObject CycleCharacter;
    public GameObject TriggeredCycle;
    public GameObject mainCh,mainChCamera,mainChUI,cycleCam,CycleUi;
    public GameObject EnterRideBtn;
    public Transform HouseChSp, HouseCameraSP;
    public GameObject Cycle;
    public GameObject FlyBtn;
    public GameObject Sun,PlayerDirectionalArrow;
    public Transform ArrowDirec;
    public GameObject env;

    void Start()
    {
        env.SetActive(true);
        GameSettings.EnforceNoShadowsOnLowRam();
        SetCycleParked(Cycle);
    }

    void SetCycleParked(GameObject cycleObject)
    {
        if (cycleObject == null)
            return;

        SimpleBike bike = cycleObject.GetComponent<SimpleBike>();
        if (bike != null)
            bike.SetParked(true);
    }

    public void AssignCh()
    {
        CycleCharacter = TriggeredCycle.GetComponent<SimpleBike>().MyDriver;
        SetCycleParked(TriggeredCycle);
    }
    public void RideCycle()
    {
        SimpleBike bike = TriggeredCycle.GetComponent<SimpleBike>();
        bike.SetParked(false);
        bike.enabled = true;
        mainCh.SetActive(false);
        mainChCamera.SetActive(false);
        mainChUI.SetActive(false);
        mainCh.transform.parent = TriggeredCycle.transform;
        CycleUi.SetActive(true);
        cycleCam.SetActive(true);
        if (CycleCharacter != null)
        {
            CycleCharacter.SetActive(true);
        }
        Invoke(nameof(StartRide), 0.25f);
        Level_Handler.instance.ReachCurrentTask();
        PlayerDirectionalArrow.transform.parent = null;
        PlayerDirectionalArrow.transform.parent = TriggeredCycle.transform;
        PlayerDirectionalArrow.transform.localPosition = new Vector3(0, 2.15f, 0);
        UpdateHNS(TriggeredCycle.transform, cycleCam);
    }
    public void StartRide()
    {
        CycleCharacter.GetComponent<Biker>().EnterExitBike();
    }
    public void ExitCycleRide()
    {
        if (TriggeredCycle != null)
        {
            SimpleBike bike = TriggeredCycle.GetComponent<SimpleBike>();
            if (bike != null)
            {
                bike.enabled = false;
                bike.SetParked(true);
            }
        }
        mainCh.SetActive(true);
        mainChCamera.SetActive(true);
        mainChUI.SetActive(true);
        mainCh.transform.parent = null;
        CycleUi.SetActive(false);
        cycleCam.SetActive(false);
        if (CycleCharacter != null)
        {
            CycleCharacter.SetActive(false);
        }
        PlayerDirectionalArrow.transform.parent = mainCh.transform;
        PlayerDirectionalArrow.transform.position = ArrowDirec.position;
        PlayerDirectionalArrow.transform.rotation = ArrowDirec.rotation;
        UpdateHNS(mainCh.transform, mainChCamera);
    }

    public void UpdateHNS(Transform playerTransform, GameObject cameraObject)
    {
        HUDNavigationSystem hns = HUDNavigationSystem.Instance;
        if (hns == null)
            return;

        if (playerTransform != null)
            hns.ChangePlayerController(playerTransform);

        if (cameraObject == null)
            return;

        Camera cam = cameraObject.GetComponent<Camera>();
        if (cam == null)
            cam = cameraObject.GetComponentInChildren<Camera>(true);
        if (cam != null)
            hns.ChangePlayerCamera(cam);
    }

    public void StopAndExitCycle()
    {
        StopCoroutine(nameof(StopAndExitCycleRoutine));
        StartCoroutine(StopAndExitCycleRoutine());
    }

    IEnumerator StopAndExitCycleRoutine()
    {
        if (TriggeredCycle == null)
            TriggeredCycle = Cycle;

        SimpleBike bike = TriggeredCycle.GetComponent<SimpleBike>();
        if (bike != null)
            bike.EmergencyStop();

        if (EnterRideBtn != null)
            EnterRideBtn.SetActive(false);
        if (CycleUi != null)
            CycleUi.SetActive(false);

        yield return new WaitForSeconds(0.35f);

        if (CycleCharacter != null)
        {
            Biker biker = CycleCharacter.GetComponent<Biker>();
            if (biker != null)
                biker.ParkAndExit();
        }

        Vector3 bikePos = TriggeredCycle.transform.position;
        Vector3 bikeRight = TriggeredCycle.transform.right;
        Quaternion bikeRot = TriggeredCycle.transform.rotation;

        ExitCycleRide();

        mainCh.transform.position = bikePos - bikeRight * 0.9f;
        mainCh.transform.rotation = bikeRot;
        mainChCamera.transform.position = bikePos - TriggeredCycle.transform.forward * 4f + Vector3.up * 1.7f;
        ThirdPersonOrbitCamBasic cam = mainChCamera.GetComponent<ThirdPersonOrbitCamBasic>();
        if (cam != null)
            cam.SnapBehindTarget();
    }
    public void SpawnOutSidePlayerHouse()
    {
        mainCh.transform.position = HouseChSp.transform.position;
        mainCh.transform.rotation = HouseChSp.transform.rotation;

        mainChCamera.transform.position = HouseCameraSP.transform.position;
        mainChCamera.transform.rotation = HouseCameraSP.transform.rotation;
    }

    public void DisableBike()
    {
        if (EnterRideBtn != null)
            EnterRideBtn.SetActive(false);
        if (CycleUi != null)
            CycleUi.SetActive(false);
        if (cycleCam != null)
            cycleCam.SetActive(false);
        if (CycleCharacter != null)
            CycleCharacter.SetActive(false);

        if (TriggeredCycle != null)
            TriggeredCycle.SetActive(false);
        if (Cycle != null)
            Cycle.SetActive(false);
    }
}
