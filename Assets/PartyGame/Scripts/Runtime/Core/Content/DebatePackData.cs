using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    [Serializable]
    public class DebateStatement
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(1, 3)] private string statement = string.Empty;
        [SerializeField] private string subcategory = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public string Id => string.IsNullOrEmpty(id) ? statement : id;
        public string Statement => statement;
        public string Subcategory => subcategory;
        public IReadOnlyList<string> Tags => tags;

        public bool IsValid => !string.IsNullOrWhiteSpace(statement);

        public DebateStatement() { }

        public DebateStatement(string id, string statement, string subcategory = "",
            IEnumerable<string> tags = null)
        {
            this.id = id;
            this.statement = statement;
            this.subcategory = subcategory;
            this.tags = tags != null ? new List<string>(tags) : new List<string>();
        }
    }

    [CreateAssetMenu(menuName = "Party Game/Content/Debate Pack", fileName = "DebatePack")]
    public class DebatePackData : ContentPack
    {
        [SerializeField] private List<DebateStatement> statements = new List<DebateStatement>();

        public IReadOnlyList<DebateStatement> Statements => statements;
        public override int EntryCount => statements.Count;

        public void SetStatements(IEnumerable<DebateStatement> newStatements)
        {
            statements = new List<DebateStatement>(newStatements);
        }
    }
}
