using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SRMultiplayer
{
    public static class UIUtils
    {
        /// <summary>
        /// SRML Function moved over here so that it can be used in
        /// </summary>
        /// <param name="mainMenu"></param>
        /// <param name="text"></param>
        /// <param name="onClicked"></param>
        /// <returns></returns>
        public static GameObject AddMainMenuButton(MainMenuUI mainMenu, string text, Action onClicked)
        {
            Transform transform = mainMenu.transform.Find("StandardModePanel/OptionsButton");
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(transform.gameObject);
            gameObject.name = text;
            gameObject.transform.SetParent(transform.parent, false);
            gameObject.transform.localPosition = new Vector3(0.0f, 0.0f);
            UnityEngine.Object.Destroy((UnityEngine.Object) gameObject.GetComponent<XlateText>());
            Button component = gameObject.GetComponent<Button>();
            component.onClick = new Button.ButtonClickedEvent();
            component.onClick.AddListener(new UnityAction(onClicked.Invoke));
            gameObject.GetComponentInChildren<TMP_Text>().text = text;
            return gameObject;
        }
    }
}