using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUp : MonoBehaviour
{
    public static PopUp active;
    [Header("Prefabs")]
    [SerializeField] 
    private GameObject loadingNotificationPrefab;
    [SerializeField] 
    private GameObject notificationPrefab;
    [SerializeField] 
    private GameObject textFieldPrefab;
    [SerializeField] 
    private GameObject boolPrefab;

    private GameObject currentPopUp;

    public void DestroyCurrentPopUp()
    {
        if (IsAPopUpActive())
        {
            Destroy(currentPopUp);
        }
    }
    public bool IsAPopUpActive(){return currentPopUp != null;}
    private void Start()
    {
        active = this;
    }
    public void LoadingNotification(string prompt)
    {
        DestroyCurrentPopUp();
        currentPopUp = Instantiate(loadingNotificationPrefab, transform);
        currentPopUp.transform.Find("Prompt").GetComponent<TMP_Text>().text = prompt;
    }
    public async Task Notification(string prompt)
    {
        DestroyCurrentPopUp();
        currentPopUp = Instantiate(notificationPrefab, transform);
        currentPopUp.transform.Find("Prompt").GetComponent<TMP_Text>().text = prompt;
        Button okButton = currentPopUp.transform.Find("ok").GetComponent<Button>();

        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        okButton.onClick.AddListener(() => tcs.TrySetResult(true));
        await tcs.Task;
        
        Destroy(currentPopUp);
    }
    public async Task<bool> BoolPrompt(string prompt)
    {
        DestroyCurrentPopUp();
        currentPopUp = Instantiate(boolPrefab, transform);
        currentPopUp.transform.Find("Prompt").GetComponent<TMP_Text>().text = prompt;
        Button trueButton = currentPopUp.transform.Find("true").GetComponent<Button>();
        Button falseButton = currentPopUp.transform.Find("false").GetComponent<Button>();
        
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        trueButton.onClick.AddListener(() => tcs.TrySetResult(true));
        falseButton.onClick.AddListener(() => tcs.TrySetResult(false));
        
        bool input = await tcs.Task;
        
        Destroy(currentPopUp);
        return input;
    }
    public async Task<string> TextInput(string prompt)
    {
        DestroyCurrentPopUp();
        currentPopUp = Instantiate(textFieldPrefab, transform);
        currentPopUp.transform.Find("Prompt").GetComponent<TMP_Text>().text = prompt;
        Button okButton = currentPopUp.transform.Find("ok").GetComponent<Button>();

        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        okButton.onClick.AddListener(() => tcs.TrySetResult(true));

        await tcs.Task;
        
        string input = currentPopUp.transform.Find("input").GetComponent<TMP_InputField>().text;
        Destroy(currentPopUp);
        return input;
    }
}
