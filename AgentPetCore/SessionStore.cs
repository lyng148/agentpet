using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentPetCore
{
    public static class AgentStateExtensions
    {
        public static int AttentionPriority(this AgentState state)
        {
            return state switch
            {
                AgentState.Working => 4,
                AgentState.Waiting => 3,
                AgentState.Done => 2,
                AgentState.Registered => 1,
                AgentState.Idle => 0,
                _ => 0
            };
        }
    }

    public class SessionStore
    {
        public TimeSpan DoneToIdleAfter { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RemoveIdleAfter { get; set; } = TimeSpan.FromSeconds(600);
        public TimeSpan StaleActiveAfter { get; set; } = TimeSpan.FromSeconds(300);
        public TimeSpan StaleRegisteredAfter { get; set; } = TimeSpan.FromSeconds(90);

        private readonly Dictionary<string, AgentSession> _byID = new();

        public void Clear()
        {
            _byID.Clear();
        }

        public void Remove(string id)
        {
            _byID.Remove(id);
        }

        public AgentSession? Apply(AgentEvent ev, DateTime now)
        {
            if (StateMapper.IsSessionEnd(ev.AgentKind, ev.EventName))
            {
                _byID.Remove(ev.SessionId);
                return null;
            }

            var state = StateMapper.State(ev.AgentKind, ev.EventName);
            if (state == null)
            {
                return null;
            }

            if (_byID.TryGetValue(ev.SessionId, out var existing))
            {
                if (existing.State != state.Value)
                {
                    existing.StateSince = now;
                }
                existing.State = state.Value;
                existing.UpdatedAt = now;
                if (ev.Project != null) existing.Project = ev.Project;
                existing.Message = ev.Message;
                return existing;
            }

            var session = new AgentSession
            {
                Id = ev.SessionId,
                AgentKind = ev.AgentKind,
                Project = ev.Project,
                State = state.Value,
                Message = ev.Message,
                Source = SessionSource.Hook,
                UpdatedAt = now,
                StateSince = now
            };
            _byID[ev.SessionId] = session;
            return session;
        }

        public void Prune(DateTime now)
        {
            var keys = _byID.Keys.ToList();
            foreach (var id in keys)
            {
                if (!_byID.TryGetValue(id, out var session)) continue;

                var quiet = now - session.UpdatedAt;
                switch (session.State)
                {
                    case AgentState.Done:
                        if (quiet >= DoneToIdleAfter)
                        {
                            session.State = AgentState.Idle;
                            session.UpdatedAt = now;
                            session.StateSince = now;
                        }
                        break;
                    case AgentState.Idle:
                        if (quiet >= RemoveIdleAfter)
                        {
                            _byID.Remove(id);
                        }
                        break;
                    case AgentState.Registered:
                        if (quiet >= StaleRegisteredAfter)
                        {
                            _byID.Remove(id);
                        }
                        break;
                    case AgentState.Working:
                    case AgentState.Waiting:
                        if (quiet >= StaleActiveAfter)
                        {
                            _byID.Remove(id);
                        }
                        break;
                }
            }
        }

        public IEnumerable<AgentSession> Sessions => _byID.Values;

        public IEnumerable<AgentSession> Sorted => _byID.Values
            .OrderByDescending(s => s.State.AttentionPriority())
            .ThenByDescending(s => s.UpdatedAt);

        public AgentSession? GetSession(string id) => _byID.TryGetValue(id, out var s) ? s : null;
    }
}
