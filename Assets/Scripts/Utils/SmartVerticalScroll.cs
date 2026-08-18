using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Freeline
{
    // Unity'nin kendi ScrollRect'ini miras alıp beynini baştan programlıyoruz!
    public class SmartVerticalScroll : ScrollRect
    {
        // Artık Inspector'dan atamamıza gerek yok, kod bunları kendi bulacak!
        private ScrollRect parentHorizontalScroll;
        private SnapScroll _parentSnapScroll;
        private bool _routeToParent;

        protected override void Awake()
        {
            base.Awake();
            
            // Hiyerarşide kendinden üstte bulunan (Ana yatay kaydırıcıdaki) SnapScroll'u otomatik bul!
            _parentSnapScroll = GetComponentInParent<SnapScroll>();

            if (_parentSnapScroll != null)
            {
                // SnapScroll'un olduğu objedeki (HorizontalSwipeView) yatay ScrollRect'i al
                parentHorizontalScroll = _parentSnapScroll.GetComponent<ScrollRect>();
            }
        }

        // Parmağı ekrana ilk koyup hareket ettirdiğimiz an (Karar anı)
        public override void OnBeginDrag(PointerEventData eventData)
        {
            // Parmağın X ekseninde mi (yatay), yoksa Y ekseninde mi (dikey) daha çok kaydığını hesapla
            _routeToParent = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);

            if (_routeToParent)
            {
                // Eğer oyuncu yana kaydırmak istiyorsa, olayı Ana Yatay Kaydırıcıya yolla
                if (parentHorizontalScroll != null)
                    parentHorizontalScroll.OnBeginDrag(eventData);
                
                if (_parentSnapScroll != null)
                    _parentSnapScroll.OnBeginDrag(eventData);
            }
            else
            {
                // Eğer oyuncu aşağı/yukarı kaydırmak istiyorsa, normal dikey ScrollRect gibi çalış
                base.OnBeginDrag(eventData);
            }
        }

        // Parmağı kaydırmaya devam ettiğimiz sürece
        public override void OnDrag(PointerEventData eventData)
        {
            if (_routeToParent)
            {
                if (parentHorizontalScroll != null)
                    parentHorizontalScroll.OnDrag(eventData);
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        // Parmağı ekrandan çektiğimiz an
        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToParent)
            {
                if (parentHorizontalScroll != null)
                    parentHorizontalScroll.OnEndDrag(eventData);
                
                // Sayfanın şak diye oturması için SnapScroll'a haber ver
                if (_parentSnapScroll != null)
                    _parentSnapScroll.OnEndDrag(eventData);
            }
            else
            {
                base.OnEndDrag(eventData);
            }
        }
    }
}