using UnityEngine;
using UnityEngine.UI;

namespace CloudFine.ThrowLab.UI
{
    [RequireComponent(typeof(Image))]
    public class UIAngleValue : MonoBehaviour
    {

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }
        public void SetRange(float range)
        {
            if (!image) Awake();
            image.fillAmount = range / 180f;
        }
    }
}
