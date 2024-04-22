Users = Users or {}

Now = Now or 0
PetTimeInterval = 1 * 60 * 60 * 1000 -- Hours for dumdum update RICORDARSI DI METTERE QUI DI NUOVO I VALORI CORRETTI!!!
NextUpdate = NextUpdate or Now

BasePetPoints = 100
StreakMultiplier = 5
ReferralPointsMultiplier = 0.1
StakedCredMultiplier = 0.01

CRED = "Sa0iBLPNyJQrwpTTG-tWLQU-1QeUAJA73DdxGGiKoJc"

TotalCurrentVotes = TotalCurrentVotes or {
    {0, 0, 0, 0, 0}, -- hat
    {0, 0, 0, 0, 0}, -- glasses
    {0, 0, 0, 0, 0}, -- coat
    {0, 0, 0, 0, 0}  -- balloon
}

DumDumState = DumDumState or {1,1,1,1}

CurrentChat = CurrentChat or {}

Version = 0.1

-- Functions

function onTick(Msg)
    Now = tonumber(Msg.Timestamp)
    if Now >= NextUpdate then  -- Time to update
        while (NextUpdate + PetTimeInterval) < Now do 
            NextUpdate = NextUpdate + PetTimeInterval --to avoid that there will be skipped updates
        end
        for walletId, info in pairs(Users) do
            if info.lastPetTime < (NextUpdate - PetTimeInterval) then
                info.petStreak = 0
            end
            info.currentVotes = {-1, -1, -1, -1}
        end
        DumDumState = getMaxIndices(TotalCurrentVotes)
        TotalCurrentVotes = {
            {0, 0, 0, 0, 0}, -- hat
            {0, 0, 0, 0, 0}, -- glasses
            {0, 0, 0, 0, 0}, -- coat
            {0, 0, 0, 0, 0}  -- balloon
        }

        CurrentChat = {}

        NextUpdate = NextUpdate + PetTimeInterval
    end
end

function getMaxIndices(votes)
    local maxIndices = {}
    for elementIndex, choices in ipairs(votes) do
        local maxIndex = 1 -- Start with the first index as the max
        local maxValue = choices[1] -- Start with the first value as the highest
        for choiceIndex = 2, #choices do
            if choices[choiceIndex] > maxValue then
                maxValue = choices[choiceIndex]
                maxIndex = choiceIndex
            end
        end
        maxIndices[elementIndex] = maxIndex
    end
    return maxIndices
end

function isNullOrEmpty(str)
    return not str or str == ""
end

function stringToTable(str)
    local result = {}
    -- Remove the curly braces and then iterate over the numbers
    str = str:sub(2, -2)  -- Cut off the first and last character to remove the braces
    for number in str:gmatch("%d+") do
        table.insert(result, tonumber(number))
    end
    return result
end

-- Handlers

-- Do OnTick after each message received
Handlers.prepend(
    "Cron-Timers",
    function(Msg)
        return "continue"
    end,
    function(Msg)
        Now = tonumber(Msg.Timestamp)
        onTick(Msg)
    end
)

Handlers.add(
    "Register",
    Handlers.utils.hasMatchingTag("Action", "Register"),
    function(Msg)
        if Users[Msg.From] then
            ao.send({
                Target = Msg.From,
                Action = "Registation-Failed",
                Data = "Already registered!"
            })
            return
        end
        
        if isNullOrEmpty(Msg.Tags["Username"]) then
            ao.send({
                Target = Msg.From,
                Action = "Registation-Failed",
                Data = "Missing name!"
            })
            return
        end

        local referral = nil
        if (not isNullOrEmpty(Msg.Tags["Referral"])) and Users[Msg.Tags["Referral"]] then
            referral = Msg.Tags["Referral"]
        end
        
        Users[Msg.From] = {
            id = Msg.From,
            username = Msg.Tags["Username"],
            referredWallet = referral,
            points = 0,
            lastPetTime = 0,
            currentVotes = {-1, -1, -1, -1},
            stakedCred = 0,
            petTimes = 0,
            petStreak = 0,
            maxPetStreak = 0
        }

        local userInfo = require("json").encode({UserInfo = Users[Msg.From]})

        ao.send({
            Target = Msg.From,
            Action = "Registration-Success",
            Data = userInfo
        })

        print(userInfo)
    end
)

Handlers.add(
    "GetUserInfo",
    Handlers.utils.hasMatchingTag("Action", "GetUserInfo"),
    function(Msg)
        local id = Msg.From
        if not Users[id] then
            ao.send({
                Target = id,
                Action = "GetUserInfo-Failed",
                Data = "User not registered!"
            })
            return
        end
        
        local userInfo = require("json").encode({
            UserInfo = Users[id]
        })

        ao.send({
            Target = id,
            Action = "GetUserInfo-Success",
            Data = userInfo
        })
    end
)

-- Gives leaderboard info.
Handlers.add(
    "GetLeaderboard",
    Handlers.utils.hasMatchingTag("Action", "GetLeaderboard"),
    function(Msg)
        local sortedUsers = {}
        for walletId, info in pairs(Users) do
            table.insert(sortedUsers, {id = walletId, username = info.username, points = info.points, petTimes = info.petTimes, petStreak = info.petStreak, maxPetStreak = info.maxPetStreak})
        end
        -- Sort the table by points in descending order
        table.sort(sortedUsers, function(a, b) return a.points > b.points end)

        local leaderboard = require("json").encode({
            Leaderboard = sortedUsers
        })

        ao.send({
            Target = Msg.From,
            Action = "Leaderboard",
            Data = leaderboard
        })
    end
)

-- Get current Votes State.
Handlers.add(
    "GetCurrentVotes",
    Handlers.utils.hasMatchingTag("Action", "GetCurrentVotes"),
    function(Msg)
        
        local currentVotes = require("json").encode({
            CurrentVotes = TotalCurrentVotes
        })

        ao.send({
            Target = Msg.From,
            Action = "CurrentVotes",
            Data = currentVotes
        })
    end
)

-- Update user votes
Handlers.add(
    "Vote",
    Handlers.utils.hasMatchingTag("Action", "Vote"),
    function(Msg)
        local walletId = Msg.From
        local newVotes = stringToTable(Msg.Data)

        if Users[walletId] then
            if table.concat(Users[walletId].currentVotes, ",") == "-1,-1,-1,-1" then
                for elementIndex, choiceIndex in ipairs(newVotes) do
                    if not (elementIndex >= 1 and elementIndex <= #TotalCurrentVotes) then
                        ao.send({
                            Target = Msg.From,
                            Action = "Vote-Failed",
                            Data = "Invalid vote!"
                        })
                        print("Invalid vote for element " .. elementIndex .. ", choice " .. choiceIndex)
                        return
                    end
                end
                
                -- Vote is valid so we can update values
                for elementIndex, choiceIndex in ipairs(newVotes) do
                    TotalCurrentVotes[elementIndex][choiceIndex] = TotalCurrentVotes[elementIndex][choiceIndex] + Users[walletId].points
                end

                Users[walletId].currentVotes = newVotes

            else
                ao.send({
                    Target = Msg.From,
                    Action = "Vote-Failed",
                    Data = "Alread voted!"
                })
                print("Already voted.")
                return
            end
        else
            ao.send({
                Target = Msg.From,
                Action = "Vote-Failed",
                Data = "User not registered!"
            })
            print("User not found.")
            return
        end

        local userInfo = require("json").encode({UserInfo = Users[Msg.From]})

        ao.send({
            Target = Msg.From,
            Action = "Vote-Success",
            Data = userInfo
        })
        print("Vote registered from " .. walletId)
    end
)

-- Handles Pet Dumdum
Handlers.add(
    "Pet",
    Handlers.utils.hasMatchingTag("Action", "Pet"),
    function(Msg)
        if not Users[Msg.From] then
            ao.send({
                Target = Msg.From,
                Action = "Pet-Failed",
                Data = "User not registered!"
            })
            print('Pet-Failed')
            return
        end

        local walletId = Msg.From

        if(Users[walletId].lastPetTime > (NextUpdate - PetTimeInterval)) then
            ao.send({Target = walletId, Action = "Pet-Failed", Data = "Wait Pet time"})
            print('Pet-Failed')
            return
        end
    
        -- Update user data
        local petPoints = math.floor(BasePetPoints + Users[walletId].stakedCred*StakedCredMultiplier + Users[walletId].petStreak*StreakMultiplier)
        Users[walletId].points = Users[walletId].points + petPoints
        Users[walletId].petTimes = Users[walletId].petTimes + 1
        Users[walletId].lastPetTime = Now
        
        Users[walletId].petStreak = Users[walletId].petStreak + 1

        if Users[walletId].maxPetStreak < Users[walletId].petStreak then
            Users[walletId].maxPetStreak = Users[walletId].petStreak
        end

        -- Add points also to referred 
        if Users[walletId].referredWallet then
            Users[Users[walletId].referredWallet].points = Users[Users[walletId].referredWallet].points + math.floor(petPoints*ReferralPointsMultiplier)
        end

        local userInfo = require("json").encode({UserInfo = Users[Msg.From]})

        ao.send({
            Target = Msg.From,
            Action = "Pet-Success",
            Data = userInfo
        })

        print('Pet-Success')
    end
)

-- Adds message into chat.
Handlers.add(
    "SendChatMessage",
    Handlers.utils.hasMatchingTag("Action", "SendChatMessage"),
    function(Msg)
        
        local id = Msg.From
        if not Users[id] then
            ao.send({
                Target = id,
                Action = "SendChatMessage-Failed",
                Data = "User not registered!"
            })
            return
        end

        -- local referral = nil
        -- if (not isNullOrEmpty(Msg.Tags["Referral"])) and Users[Msg.Tags["Referral"]] then
        --     referral = Msg.Tags["Referral"]
        -- end

        local messageEntry = {
            Sender = id,
            Username = Users[id].username,
            Message = Msg.Data,
            Timestamp = Msg.Timestamp
        }

        table.insert(CurrentChat, messageEntry)

        local message = require("json").encode({
            MessageData = messageEntry
        })

        ao.send({
            Target = id,
            Action = "SendChatMessage-Success",
            Data = message
        })
    end
)

-- Gives Current Chat info.
Handlers.add(
    "GetCurrentChat",
    Handlers.utils.hasMatchingTag("Action", "GetCurrentChat"),
    function(Msg)
        
        local currentChat = require("json").encode({
            CurrentChat = CurrentChat
        })

        ao.send({
            Target = Msg.From,
            Action = "CurrentChat",
            Data = currentChat
        })
    end
)

Handlers.add(
    "GetDumDumInfo",
    Handlers.utils.hasMatchingTag("Action", "GetDumDumInfo"),
    function (Msg)
        local json = require("json")
        local DumDumInfo = json.encode({ 
            DumDumInfo = {
            PetTimeInterval = PetTimeInterval,
            NextUpdate = NextUpdate,
            BasePetPoints = BasePetPoints,
            ReferralPointsMultiplier = ReferralPointsMultiplier,
            StreakMultiplier = StreakMultiplier,
            StakedCredMultiplier = StakedCredMultiplier,
            Version = Version,
            DumDumState = DumDumState
        }
        })
        Send({
            Target = Msg.From,
            Action = "DumDumInfo",
            Data = DumDumInfo
        })
    end
)