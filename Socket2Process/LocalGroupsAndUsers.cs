using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Socket2Process
{
    public class LocalGroupsAndUsers
    {
        public static string BuiltinAdminGroup =
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).ToString(); // "S-1-5-32-544";

        public static void PrintInfo(Action<string> log)
        {
            var machineContext = new PrincipalContext(ContextType.Machine);
            var userPrincipal = new UserPrincipal(machineContext);
            var userPrincipalSearcher = new PrincipalSearcher(userPrincipal);

            foreach (var user in userPrincipalSearcher.FindAll())
            {
                log(string.Format("[User] Name: {0}\tSID:{1}", user.Name, user.Sid.ToString()));
                foreach (var group in user.GetGroups())
                {
                    log(string.Format("\t[Group] Name: {0}\tSID:{1}", group.Name, group.Sid.ToString()));
                }
            }
        }

        public static void getLimitedUsers(Action<string, string> callbackNameAndSid)
        {
            var machineContext = new PrincipalContext(ContextType.Machine);
            var userPrincipal = new UserPrincipal(machineContext);
            var userPrincipalSearcher = new PrincipalSearcher(userPrincipal);

            foreach (var user in userPrincipalSearcher.FindAll())
            {
                //log(string.Format("[User] Name: {0}\tSID:{1}", user.Name, user.Sid.ToString()));

                bool isAdmin = false;
                foreach (var group in user.GetGroups())
                {
                    //log(string.Format("\t[Group] Name: {0}\tSID:{1}", group.Name, group.Sid.ToString()));
                    if (group.Sid.ToString() == BuiltinAdminGroup)
                    {
                        isAdmin = true;
                        break;
                    }
                }

                if (!isAdmin)
                {
                    callbackNameAndSid(user.Name, user.Sid.ToString());
                }
            }
        }


        Dictionary<string, HashSet<string>> UserSidGroupSidMap = new Dictionary<string, HashSet<string>>();
        Dictionary<string, HashSet<string>> UserSidGroupNameMap = new Dictionary<string, HashSet<string>>();
        Dictionary<string, string> UserSidNameMap = new Dictionary<string, string>();
        Dictionary<string, string> GroupSidNameMap = new Dictionary<string, string>();

        public LocalGroupsAndUsers(bool init=true)
        {
            if (init)
                refreshAll();
        }

        public void refreshAll()
        {
            UserSidGroupSidMap = new Dictionary<string, HashSet<string>>();
            UserSidGroupNameMap = new Dictionary<string, HashSet<string>>();
            UserSidNameMap = new Dictionary<string, string>();
            GroupSidNameMap = new Dictionary<string, string>();

            var machineContext = new PrincipalContext(ContextType.Machine);
            var userPrincipal = new UserPrincipal(machineContext);
            var userPrincipalSearcher = new PrincipalSearcher(userPrincipal);

            foreach (var user in userPrincipalSearcher.FindAll())
            {
                string userSid = user.Sid.ToString();
                UserSidNameMap.Add(userSid, user.Name);

                foreach (var group in user.GetGroups())
                {
                    string groupSid = group.Sid.ToString();

                    if (!GroupSidNameMap.ContainsKey(groupSid))
                    {
                        GroupSidNameMap.Add(groupSid, group.Name);
                    }

                    if (!UserSidGroupSidMap.ContainsKey(userSid))
                    {
                        UserSidGroupSidMap.Add(userSid, new HashSet<string>());
                    }
                    UserSidGroupSidMap[userSid].Add(groupSid);

                    if (!UserSidGroupNameMap.ContainsKey(userSid))
                    {
                        UserSidGroupNameMap.Add(userSid, new HashSet<string>());
                    }
                    UserSidGroupNameMap[userSid].Add(group.Name);
                }
            }
        }

        public string getUserName(string sid)
        {
            string result = "";
            if (UserSidNameMap.ContainsKey(sid))
                result = UserSidNameMap[sid];
            return result;
        }

        public string getGroupName(string sid)
        {
            string result = "";
            if (GroupSidNameMap.ContainsKey(sid))
                result = GroupSidNameMap[sid];
            return result;
        }

        public bool isUserInGroups(string sid, string[] groupSids, string[] groupNames)
        {
            bool found = false;
            if (!UserSidGroupSidMap.ContainsKey(sid) || !UserSidGroupNameMap.ContainsKey(sid))
                return false;

            HashSet<string> userGroupsSid = UserSidGroupSidMap[sid];
            HashSet<string> userGroupsNames = UserSidGroupNameMap[sid];

            for (int i = 0; i < groupSids.Length; i++)
            {
                if (userGroupsSid.Contains(groupSids[i]))
                {
                    found = true;
                    break;
                }    
            }

            if (!found)
            {
                for (int i = 0; i < groupNames.Length; i++)
                {
                    if (userGroupsNames.Contains(groupNames[i]))
                    {
                        found = true;
                        break;
                    }
                }
            }

            return found;
        }

    }
}
