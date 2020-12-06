using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Xml.Xsl;

namespace Fireguard
{
    class Program
    {
        private static Dictionary<int, List<int>> records = new Dictionary<int, List<int>>();
        private static Dictionary<int, string> dayToDate = new Dictionary<int, string>();
        private static Dictionary<int, Person> personLookup = new Dictionary<int, Person>();
        
        static void Main(string[] args)
        {
            using var writer = new StreamWriter($"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");
            var day = 1;
            var persons = PopulatePersons(ref day);
            foreach (var person in persons)
            {
                personLookup.Add(person.Id, person);
            }
            for (; day <= 50; day++)
            {
                // GenerateSchedule(persons, 1, day);
                // GenerateSchedule(persons, 2, day);
                // BalanceTeams(persons, day);
                var result = new List<Person>();
                GenerateSchedule(persons, 1, day, result);
                GenerateSchedule(persons, 2, day, result);
                Shuffle(result);
                result.Sort((a, b) => a.Team - b.Team);
                foreach (var person in result)
                {
                    writer.Write($"{person.Id},");
                }
                writer.WriteLine();
            }
            
            LoadDayToDate();
            
            var dump = new List<KeyValuePair<int, List<int>>>();
            foreach (var k in records) dump.Add(k);
            dump.Sort((a, b) => a.Key - b.Key);
            foreach (var (key, value) in dump)
            {
                writer.Write($"{key}\t");
                writer.Write($"{dayToDate[personLookup[key].LeaveDay]}\t");
                var last = -1000;
                var smallestGap = 100;
                foreach (var d in value)
                {
                    writer.Write($"{dayToDate[d]}\t");
                    smallestGap = Math.Min(smallestGap, d - last);
                    last = d;
                }
                writer.WriteLine();
                //writer.WriteLine($"Smallest Gap: {smallestGap}");
            }
            
            var cnt = new Dictionary<int, int>();
            foreach (var (key, value) in records)
            {
                if (!cnt.TryAdd(value.Count, 1)) cnt[value.Count]++;
            }

            foreach (var (key, value) in cnt)
            {
                Console.WriteLine($"{key} times: {value} persons");
            }
        }

        private static void LoadDayToDate()
        {
            using var reader = new StreamReader("d2d.csv");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var strings = line.Split(',');
                dayToDate.Add(int.Parse(strings[0]), strings[1]);
            }
        }

        private static void GenerateSchedule(List<Person> persons, int team, int day, List<Person> result)
        {
            var candidates = new List<Person>();
            int minGap = 4;
            while (candidates.Count < 8 && minGap >= 2)
            {
                candidates.Clear();
                foreach (var person in persons)
                {
                    if (person.LeaveDay >= day && day - person.LastDay >= minGap && person.TimesDone <= 7)
                    {
                        candidates.Add(person);
                    }
                }

                minGap--;
            }

            candidates.Sort((a, b) =>
            {
                var ar = (double) a.TimesLeft / (a.LeaveDay - day + 2);
                var br = (double) b.TimesLeft / (b.LeaveDay - day + 2);
                if (ar != br) return (ar < br) ? 1 : -1;
                if (a.TimesDone != b.TimesDone) return a.TimesDone - b.TimesDone;
                if (a.Team != b.Team) return team == 1 ? a.Team - b.Team : b.Team - a.Team;
                return a.TieBreaker - b.TieBreaker;
            });

            for (var i = 0; i < 8 && i < candidates.Count; i++)
            {
                candidates[i].LastDay = day;
                candidates[i].TimesLeft--;
                candidates[i].TimesDone++;
                records[candidates[i].Id].Add(day);
                result.Add(candidates[i]);
            }
        }

        private static void BalanceTeams(List<Person> persons, int day)
        {
            var team1 = new List<Person>();
            var team2 = new List<Person>();
            foreach (var person in persons)
            {
                if (person.LeaveDay <= day) continue;
                if(person.Team == 1) team1.Add(person);
                else team2.Add(person);
            }
            
            var rand = new Random();
            var team1Cnt = team1.Count;
            var team2Cnt = team2.Count;
            while (Math.Abs(team1Cnt - team2Cnt) > 1)
            {
                if (team1Cnt > team2Cnt)
                {
                    var target = -1;
                    do
                    {
                        target = rand.Next(team1.Count);
                    } while (team1[target].Team != 1);
                    //Console.WriteLine($"Person {team1[target].Id} is now on Team 2");
                    team1[target].Team = 2;
                    team1Cnt--;
                    team2Cnt++;
                }
                else
                {
                    var target = -1;
                    do
                    {
                        target = rand.Next(team2.Count);
                    } while (team2[target].Team != 2);
                    //Console.WriteLine($"Person {team2[target].Id} is now on Team 1");
                    team2[target].Team = 1;
                    team2Cnt--;
                    team1Cnt++;
                }
            }
            persons.Clear();
            foreach (var person in team1)
            {
                persons.Add(person);
            }
            foreach (var person in team2)
            {
                persons.Add(person);
            }

            Shuffle(persons);
        }
        
        private static readonly Random rng = new Random();

        private static void Shuffle(IList<Person> persons)
        {
            var n = persons.Count;  
            while (n > 1) {  
                n--;  
                var k = rng.Next(n + 1);  
                var value = persons[k];  
                persons[k] = persons[n];  
                persons[n] = value;  
            }  
        }

        private static List<Person> PopulatePersons(ref int day)
        {
            var ret = new List<Person>();
            {
                var cnt = new Dictionary<int, int>();
                var lastDay = new Dictionary<int, int>();
                var team = new Dictionary<int, int>();
                var leaveDay = new Dictionary<int, int>();
                
                {
                    using var reader = File.OpenText("lst.csv");
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var arr = line.Split(','); // id,team,leave_day
                        var n = int.Parse(arr[0]);
                        team.Add(n, int.Parse(arr[1]));
                        leaveDay.Add(n, int.Parse(arr[2]));
                    }
                }
                
                {
                    using var reader = File.OpenText("rec.csv");
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var nums = line.Split(',');
                        foreach (var num in nums)
                        {
                            var n = int.Parse(num);
                            if (!team.ContainsKey(n)) continue;
                            if (!cnt.TryAdd(n, 1))
                            {
                                cnt[n]++;
                            }
                            if (!lastDay.TryAdd(n, day))
                            {
                                lastDay[n] = day;
                            }

                            records.TryAdd(n, new List<int>());
                            records[n].Add(day);
                        }
                        day++;
                    }
                }

                foreach (var (key, value) in cnt)
                {
                    var left = 7 - value;
                    var newPerson = new Person(key, team[key], value, left, lastDay[key], leaveDay[key]);
                    ret.Add(newPerson);
                }
                
                ret.Sort((a, b) => a.Id - b.Id);
            }
            Shuffle(ret);
            return ret;
        }
    }
}