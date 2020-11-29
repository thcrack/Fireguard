using System;

namespace Fireguard
{
    public class Person
    {
        public int Id;
        public int Team;
        public int TimesDone;
        public int TimesLeft;
        public int LastDay;
        public int LeaveDay;
        public int TieBreaker;
        private static Random rng = new Random();

        public Person(int id, int team, int timesDone, int timesLeft, int lastDay, int leaveDay)
        {
            Id = id;
            Team = team;
            TimesDone = timesDone;
            TimesLeft = timesLeft;
            LastDay = lastDay;
            LeaveDay = leaveDay;
            TieBreaker = rng.Next();
        }

        public override string ToString()
        {
            return $"Id: {Id}, Team: {Team}, TimesDone: {TimesDone}, TimesLeft: {TimesLeft}, LastDay: {LastDay}, LeaveDay: {LeaveDay}";
        }
    }
}