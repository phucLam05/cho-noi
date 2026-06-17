using UnityEngine;
using System.Collections.Generic;

namespace ChoNoi.Infrastructure
{
    [System.Serializable]
    public class DialogueNode
    {
        public string id;
        public string speakerName;
        [TextArea(3, 5)]
        public string dialogueText;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextNodeId; // Empty means end conversation
        public DialogueAction action; // Action like Haggle, GiveGift
    }

    public enum DialogueAction
    {
        None,
        Haggle,
        GiveGift,
        AcceptDeal,
        DeclineDeal,
        BuyFood
    }

    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "ChoNoi/Data/Dialogue Data", order = 2)]
    public class DialogueData : ScriptableObject
    {
        public Sprite npcAvatar;
        public string initialNodeId;
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }
    }
}
