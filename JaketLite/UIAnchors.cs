using Polarite.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Polarite
{
    public static class UIAnchors
    {
        public static RectTransform Chat;
        public static RectTransform VC;
        private static RectTransform chatPanel;
        private static RectTransform vcHud;
        private static GameObject chatAnchors;
        private static GameObject vcAnchors;

        public static void SetChat(RectTransform chUI, RectTransform panel)
        {
            Chat = chUI;
            chatPanel = panel;
            chatAnchors = Chat.transform.Find("AnchorPoints").gameObject;
            Refresh();
        }
        public static void SetVC(RectTransform vcUI, RectTransform hud)
        {
            VC = vcUI;
            vcHud = hud;
            vcAnchors = VC.transform.Find("AnchorPoints").gameObject;
            Refresh();
        }
        public static void Refresh(ChatAlign chat)
        {
            if (Chat != null)
            {
                switch (chat)
                {
                    case ChatAlign.TopLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("TopLeft").position;
                        break;
                    case ChatAlign.MiddleLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("CenterLeft").position;
                        break;
                    case ChatAlign.BottomLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("BottomLeft").position;
                        break;
                    case ChatAlign.TopMiddle:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("Top").position;
                        break;
                    case ChatAlign.BottomMiddle:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("Bottom").position;
                        break;
                    case ChatAlign.TopRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("TopRight").position;
                        break;
                    case ChatAlign.MiddleRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("CenterRight").position;
                        break;
                    case ChatAlign.BottomRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("BottomRight").position;
                        break;
                }
            }
        }
        public static void Refresh(VCAlign vc)
        {
            if (VC != null)
            {
                switch(vc)
                {
                    case VCAlign.Left:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Left").position;
                        break;
                    case VCAlign.Middle:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Center").position;
                        break;
                    case VCAlign.Right:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Right").position;
                        break;
                    }
                }
        }
        public static void Refresh(VCListAlign vcList)
        {
            if (VC != null)
            {
                if (vcHud.TryGetComponent<VerticalLayoutGroup>(out var vcGroup))
                {
                    switch (vcList)
                    {
                        case VCListAlign.TopToBottom:
                            vcGroup.childAlignment = TextAnchor.UpperCenter;
                            break;
                        case VCListAlign.Center:
                            vcGroup.childAlignment = TextAnchor.MiddleCenter;
                            break;
                        case VCListAlign.BottomToTop:
                            vcGroup.childAlignment = TextAnchor.LowerCenter;
                            break;
                    }
                }
            }
        }
        public static void Refresh()
        {
            if(Chat != null)
            {
                switch (ItePlugin.chatAlignment.value)
                {
                    case ChatAlign.TopLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("TopLeft").position;
                        break;
                    case ChatAlign.MiddleLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("CenterLeft").position;
                        break;
                    case ChatAlign.BottomLeft:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("BottomLeft").position;
                        break;
                    case ChatAlign.TopMiddle:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("Top").position;
                        break;
                    case ChatAlign.BottomMiddle:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("Bottom").position;
                        break;
                    case ChatAlign.TopRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("TopRight").position;
                        break;
                    case ChatAlign.MiddleRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("CenterRight").position;
                        break;
                    case ChatAlign.BottomRight:
                        chatPanel.position = chatAnchors.FindWithComponent<RectTransform>("BottomRight").position;
                        break;
                }
            }
            if(VC != null)
            {
                switch (ItePlugin.vcAlignmentPos.value)
                {
                    case VCAlign.Left:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Left").position;
                        break;
                    case VCAlign.Middle:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Center").position;
                        break;
                    case VCAlign.Right:
                        vcHud.position = vcAnchors.FindWithComponent<RectTransform>("Right").position;
                        break;
                }
                if(vcHud.TryGetComponent<VerticalLayoutGroup>(out var vcGroup))
                {
                    switch (ItePlugin.vcAlignmentList.value)
                    {
                        case VCListAlign.TopToBottom:
                            vcGroup.childAlignment = TextAnchor.UpperCenter;
                            break;
                        case VCListAlign.Center:
                            vcGroup.childAlignment = TextAnchor.MiddleCenter;
                            break;
                        case VCListAlign.BottomToTop:
                            vcGroup.childAlignment = TextAnchor.LowerCenter;
                            break;
                    }
                }
            }
        }
    }
}
