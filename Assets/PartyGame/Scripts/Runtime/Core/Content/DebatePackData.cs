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

        public string Id => string.IsNullOrEmpty(id) ? statement : id;
        public string Statement => statement;
        public bool IsValid => !string.IsNullOrWhiteSpace(statement);

        public DebateStatement() { }

        public DebateStatement(string id, string statement)
        {
            this.id = id;
            this.statement = statement;
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
