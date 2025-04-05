using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Assets.SimpleSpinner;


public class AvatarCondition : MonoBehaviour
{
    public GameObject avatar; // Assign your avatar in the Unity Inspector
    public AudioClip[] voiceClips; // Audio clips in the same order as promptsAgent
    private AudioSource audioSource;

    // GameObject valve;
    GameObject[] valves;
    // GameObject[] pipeTasks;
    List<GameObject> pipeTasks = new List<GameObject>();
    GameObject arrow;
    GameObject targetValve;
    GameObject targetPipe;
    GameObject targetParticleSystem;
    GameObject lastIndicator;

    GameObject textContainer;
    GameObject buttonContainer;

    TextMeshProUGUI titleText;
    TextMeshProUGUI detailText;
    TextMeshProUGUI btnText;

    public GameObject indicator;
    public GameObject indicatorWithoutInteraction;

    private bool isGrabbing = false;
    private float grabTime = 0f;
    private Coroutine grabCoroutine;

    int status = 0;
    // 0: Welcome *
    // 1: Task 1 instruction *
    // 2: Task 1
    // 3: When closing all valves *
    // 4: Task 2 instruction *
    // 5: Task 2
    // 6: Task 2 done *

    bool isAvatar = true;

    string[,] promptsText = new string[,] {
        { "Welcome to the Pipeline Training Facility", "In this condition, you will be able to control valve rotation and identify pipe cracks.This is a text prompt based condition, so please follow the written instructions infront you to complete the tasks.", "Next" }, //0
        { "Task 1: Closing valve", "In this task, you will rotate 10 pipeline valves to turn off the flow in the pipeline facility. Follow the direction of the arrows to locate correct valve and pinch the knob for 5 seconds to turn off.", "Next"  }, //1
        { "", "", "" }, //2
        { "Task 1: Completed", "Well done! You have successfully shut down all the valves. Let's move on to the next stage in our training.", "Next" }, //3
        { "Task 2: Identifying Pipeline cracks", "In this task, you will locate the pipeline crack by following the arrow and investigating the leak. Once located, you will fix the crack using the object present in the panel infront of you.", "Next" }, //4
        { "", "", ""  }, //5
        { "End of Task 2", "Great job! You have fixed all the cracks!", "Next" }, //6
        { "The end", "Thank you for following the written  instructions and safely completing your pipeline training. We hope you have learnt the basics of pipeline operations.", "Done" } //7
    };

    string[,] promptsAgent = new string[,] {
        { "Welcome to the Pipeline Training Facility", "In this condition, you will be able to control valve rotation and identify pipe cracks.This is a virtual agent based condition, so please follow the verbal instructions given to you by the agent to complete the tasks", "Next" }, //0
        { "Task 1: Closing valve", "In this task, you will rotate 10 pipeline valves to turn off the flow in the pipeline facility. Follow the direction of the arrows to locate correct valve and pinch the knob for 5 seconds to turn off.", "Next"  }, //1
        { "", "", "" }, //2
        { "Task 1: Completed", "Well done! You have successfully shut down all the valves. Let's move on to the next stage in our training.", "Next" }, //3
        { "Task 2: Identifying Pipeline cracks", "In this task, you will locate the pipeline crack by following the arrow and investigating the leak. Once located, you will fix the crack using the object present in the panel infront of you.", "Next" }, //4
        { "", "", ""  }, //5
        { "End of Task 2", "Great job! You have fixed all the cracks!", "Next" }, //6
        { "The end", "Thank you for following the agent's instructions and safety completing your pipeline training. We hope you have learnt the basics of pipeline operations.", "Done" } //7
    };

    int closeValveCount = 0;
    int MaxCloseValveCount = 4;

    int fixPipeCount = 0;
    int MaxFixPipeCount = 3;

    // Start is called before the first frame update
    void Start()
    { 

        // valve = GameObject.FindWithTag("valve");
        valves = GameObject.FindGameObjectsWithTag("valve");
        pipeTasks = GameObject.FindGameObjectsWithTag("pipeTask").ToList();
        arrow = GameObject.FindWithTag("arrow");

        if (GameObject.FindWithTag("title") != null)
        {
            titleText = GameObject.FindWithTag("title").GetComponent<TextMeshProUGUI>();
        }

        if (GameObject.FindWithTag("detail") != null)
        {
            detailText = GameObject.FindWithTag("detail").GetComponent<TextMeshProUGUI>();
        }

        if (GameObject.FindWithTag("btnText") != null)
        {
            btnText = GameObject.FindWithTag("btnText").GetComponent<TextMeshProUGUI>();
        }

        //Hide all pipe tasks at the beginning
        foreach (GameObject pipeline in pipeTasks)
        {
            pipeline.SetActive(false);
        }

        textContainer = GameObject.Find("TextContainer");
        buttonContainer = GameObject.Find("ButtonContainer");

        titleText.text = promptsText[0, 0];
        detailText.text = promptsText[0, 1];

        if (avatar != null)
        {
            audioSource = avatar.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = avatar.AddComponent<AudioSource>();
            }
        }

        if (isAvatar && textContainer != null)
        {
            textContainer.SetActive(false);
        }

        if (isAvatar && voiceClips != null && voiceClips.Length > 0 && voiceClips[0] != null)
        {
            if (avatar != null)
            {
                avatar.SetActive(true);
            }

            audioSource.Stop();
            audioSource.clip = voiceClips[0];
            audioSource.Play();
            Debug.Log("Playing first audio clip at start.");
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (isAvatar && avatar != null && audioSource != null && audioSource.isPlaying)
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 direction = cameraTransform.position - avatar.transform.position;
            direction.y = 0; // Prevent tilting
            avatar.transform.rotation = Quaternion.LookRotation(direction);
        }

        if (arrow != null)
        {
            if (arrow.activeSelf == false) arrow.SetActive(true);
            if (targetValve != null)
            {
                arrow.transform.LookAt(targetValve.transform);
            }
            else if (targetParticleSystem != null)
            {
                arrow.transform.LookAt(targetParticleSystem.transform);
            }
        }

        if (arrow != null && targetValve == null && targetParticleSystem == null)
        {
            arrow.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            next();
            return;
            if (status == 2)
            {
                resetTargetValve();
            }

            if (status == 5)
            {
                //
            }
        }
    }

    public void onEnteredGrabValve(GameObject go)
    {
        if (go != null)
        {
            Debug.Log("onEnteredGrabValve: " + go.name);
        }
    }

    public void onExitGrabValve(GameObject go)
    {
        if (go != null)
        {
            Debug.Log("onExitGrabValve: " + go.name);
        }
    }

    // 0: Welcome *
    // 1: Task 1 instruction *
    // 2: Task 1
    // 3: When closing all valves *
    // 4: Task 2 instruction *
    // 5: Task 2
    // 6: Task 2 done *

    public void goTo(int targetStatus)
    {

        status = targetStatus - 1;

        next();
    }

    public void next()
    {
        Debug.Log($"status: {status}");

        status++;

        if (status > 7)
        {
            return;
        }

        // Handle avatar visibility
        if (avatar != null)
        {
            if (status == 2 || status == 5)
            {
                avatar.SetActive(false);
                Debug.Log("Avatar hidden during active task.");
            }
            else
            {
                avatar.SetActive(true);
                Debug.Log("Avatar visible.");
            }
        }
        if (textContainer != null)
        {
            textContainer.SetActive(!isAvatar);
        }

        if (isAvatar)
        {
            titleText.text = promptsAgent[status, 0];
            detailText.text = promptsAgent[status, 1];
            btnText.text = promptsAgent[status, 2];

            // Play corresponding audio if available
            if (voiceClips != null && status < voiceClips.Length && voiceClips[status] != null)
            {
                audioSource.Stop();
                audioSource.clip = voiceClips[status];
                audioSource.Play();
            }
            if (textContainer != null)
            {
                textContainer.SetActive(false);
            }
        }
        else
        {
            titleText.text = promptsText[status, 0];
            detailText.text = promptsText[status, 1];
            btnText.text = promptsText[status, 2];
        }

        if (status == 2)
        {
            resetTargetValve();
            if (textContainer != null) textContainer.SetActive(false);
            if (buttonContainer != null) buttonContainer.SetActive(false);
        }

        if (status == 3)
        {
            if (textContainer != null) textContainer.SetActive(!isAvatar);
            if (buttonContainer != null) buttonContainer.SetActive(true);
        }

        if (status == 5)
        {
            resetTargetPipe();
            if (textContainer != null) textContainer.SetActive(false);
            if (buttonContainer != null) buttonContainer.SetActive(false);
        }

        if (status == 6)
        {
            if (textContainer != null) textContainer.SetActive(!isAvatar);
            if (buttonContainer != null) buttonContainer.SetActive(true);
        }
    }

    public void onEnteredGrabSpinner()
    {
        isGrabbing = true;
        grabCoroutine = FindObjectOfType<EventManager>().StartCoroutine(CheckGrabDuration());
        Debug.Log("onEnteredGrabSpinner");

        GameObject[] spinnerCountContainers = GameObject.FindGameObjectsWithTag("spinnerCountContainer");

        foreach (GameObject spinnerCountContainer in spinnerCountContainers)
        {
            foreach (Transform child in spinnerCountContainer.transform)
            {
                if (child.CompareTag("spinner"))
                {
                    if (child.gameObject != null)
                    {
                        SimpleSpinner ss = child.gameObject.GetComponent<SimpleSpinner>();
                        if (ss != null) ss.enabled = true;
                    }
                }
                if (child.CompareTag("countContainer"))
                {
                    if (child.gameObject != null)
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    public void onExitGrabSpinner()
    {
        Debug.Log("onExitGrabSpinner");

        GameObject[] spinnerCountContainers = GameObject.FindGameObjectsWithTag("spinnerCountContainer");

        foreach (GameObject spinnerCountContainer in spinnerCountContainers)
        {
            foreach (Transform child in spinnerCountContainer.transform)
            {
                if (child.CompareTag("spinner"))
                {
                    if (child.gameObject != null)
                    {
                        SimpleSpinner ss = child.gameObject.GetComponent<SimpleSpinner>();
                        if (ss != null) ss.enabled = false;
                    }
                }
                if (child.CompareTag("countContainer"))
                {
                    if (child.gameObject != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        isGrabbing = false;
        grabTime = 0f;
        if (grabCoroutine != null)
        {
            FindObjectOfType<EventManager>().StopCoroutine(grabCoroutine);
            grabCoroutine = null;
        }
    }

    private IEnumerator CheckGrabDuration()
    {
        GameObject[] spinnerCountContainers = GameObject.FindGameObjectsWithTag("spinnerCountContainer");
        grabTime = 0f;

        while (isGrabbing)
        {
            grabTime += Time.deltaTime;
            foreach (GameObject spinnerCountContainer in spinnerCountContainers)
            {
                foreach (Transform child in spinnerCountContainer.transform)
                {
                    if (child.CompareTag("countContainer"))
                    {
                        if (child.gameObject != null)
                        {
                            foreach (Transform _child in child.gameObject.transform)
                            {
                                if (_child.CompareTag("holdCount"))
                                {
                                    TextMeshProUGUI tmpug = _child.GetComponent<TextMeshProUGUI>();
                                    if (tmpug != null)
                                    {
                                        tmpug.text = grabTime.ToString("F2");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (grabTime >= 5f)
            {
                FindObjectOfType<EventManager>().isGoToNextTask();
                yield break; // Stop checking further
            }
            yield return null;
        }
    }

    public void resetTargetValve()
    {
        if (lastIndicator != null)
        {
            Destroy(lastIndicator, 0.0f);
        }
        targetValve = valves[(Random.Range(0, valves.Length))];

        Debug.Log($"Target Valve Name: {targetValve.name}");

        if (indicator != null && targetValve != null)
        {
            lastIndicator = Instantiate(indicator, targetValve.transform);

            lastIndicator.transform.Rotate(new Vector3(0, 90, -90));
            // myInstance.transform.rotation = Quaternion.Euler (new Vector3(180, 0, 0));
            lastIndicator.transform.localPosition = new Vector3(0, 0, -1);
        }
    }

    public void ManipulationEnded(GameObject go)
    {
        targetParticleSystem = null;

        Debug.Log("ManipulationEnded");
        GameObject targetPipeline = GameObject.FindWithTag("targetPipeline");

        float distance = Vector3.Distance(go.transform.position, targetPipeline.transform.position);

        // 회전 비교
        float angle = Quaternion.Angle(go.transform.rotation, targetPipeline.transform.rotation);

        // 위치와 회전 모두 비교
        if (distance < 0.1f && angle < 1f)
        {
            go.transform.position = targetPipeline.transform.position;
            go.transform.rotation = targetPipeline.transform.rotation;

            // Destroy(targetPipeline);
            Destroy(targetPipe);
            pipeTasks.Remove(targetPipe);
            fixPipeCount++;
            resetTargetPipe();
        }
    }

    public void resetTargetPipe()
    {
        if (pipeTasks.Count == 0)
        {
            next();
            return;
        }
        if (lastIndicator != null)
        {
            Destroy(lastIndicator, 0.0f);
        }

        targetPipe = pipeTasks[(Random.Range(0, pipeTasks.Count))];

        Debug.Log($"Target Pipe Name: {targetPipe.name}");

        if (indicatorWithoutInteraction != null && targetPipe != null)
        {
            targetPipe.SetActive(true);

            foreach (Transform particleSystem in targetPipe.transform)
            {
                if (particleSystem.CompareTag("particleSystem"))
                {
                    Debug.Log(particleSystem.name);
                    targetParticleSystem = particleSystem.gameObject;
                    lastIndicator = Instantiate(indicatorWithoutInteraction, targetParticleSystem.transform);

                    // lastIndicator.transform.Rotate(new Vector3(0, 90, -90));
                    // myInstance.transform.rotation = Quaternion.Euler (new Vector3(180, 0, 0));
                    lastIndicator.transform.localPosition = new Vector3(0, 7, 0);
                }
            }
        }
    }

    public void increaseCloseValveCount()
    {
        closeValveCount++;
    }

    public void isGoToNextTask()
    {
        increaseCloseValveCount();
        Debug.Log(closeValveCount);
        if (closeValveCount > MaxCloseValveCount)
        {
            //Go-To Next Tasks;
            if (lastIndicator != null)
            {
                Destroy(lastIndicator, 0.0f);
            }
            targetValve = null;
            Debug.Log("Go To Next Task");
            next();
        }
        else
        {
            //Keep doing until MaxCloseValveCount
            resetTargetValve();
        }
    }
}