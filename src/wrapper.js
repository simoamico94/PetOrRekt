import { sendMessage } from './sendMessage.js'
import { spawnProcess } from './spawnProcess.js'
import { connectArweaveWallet, fetchProcesses } from './connectWallet.js'
import { dumdumRegisterReferral, dumdumRegister, dumdumPet, dumdumVote, dumdumSendChat } from './dumdum.js'
import { requestNotificationPermission, sendNotification } from './utils.js'
import { downloadImage, shareOnTwitter } from './screenshot.js'

export const UnityAO = {
    sendMessage,
    spawnProcess,
    connectArweaveWallet,
    fetchProcesses,
    dumdumRegisterReferral,
    dumdumRegister,
    dumdumPet,
    dumdumVote,
    dumdumSendChat,
    requestNotificationPermission,
    sendNotification,
    downloadImage,
    shareOnTwitter
}
