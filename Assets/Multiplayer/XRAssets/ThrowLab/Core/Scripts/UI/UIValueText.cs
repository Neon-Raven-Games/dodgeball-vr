using TMPro;
using UnityEngine;

namespace CloudFine.ThrowLab.UI
{
    public class UIValueText : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        public string _preDecorator = "";
        public string _toStringPattern = "0.0";
        public string _postDecorator = "";

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        public void SetValue(float value)
        {
            if (!_text) Awake();
            _text.text = _preDecorator + value.ToString(_toStringPattern) + _postDecorator;
        }
    }
}