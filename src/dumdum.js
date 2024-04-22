import { connect, createDataItemSigner } from '@permaweb/aoconnect'

export async function dumdumRegisterReferral(pid, username, referral) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: 'Register' }, { name: 'Username', value: username }, { name: 'Referral', value: referral }],
        data: ''
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error) {
        myUnityInstance.SendMessage('SocialManager', 'RegistrationCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Messages[0].Data) {
        myUnityInstance.SendMessage('SocialManager', 'RegistrationCallback', result.Messages[0].Data);
        return result.Messages[0].Data
    }

    return undefined;
}

export async function dumdumRegister(pid, username) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: 'Register' }, { name: 'Username', value: username }],
        data: ''
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error) {
        myUnityInstance.SendMessage('SocialManager', 'RegistrationCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Messages[0].Data) {
        myUnityInstance.SendMessage('SocialManager', 'RegistrationCallback', result.Messages[0].Data);
        return result.Messages[0].Data
    }

    return undefined;
}

export async function dumdumPet(pid) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: 'Pet' }],
        data: ''
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error) {
        myUnityInstance.SendMessage('SocialManager', 'PetCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Messages[0].Data) {
        console.log(result.Messages[0].Data);
        myUnityInstance.SendMessage('SocialManager', 'PetCallback', result.Messages[0].Data);
        return result.Messages[0].Data
    }

    return undefined;
}

export async function dumdumVote(pid, data) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: 'Vote' }],
        data: data
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error) {
        myUnityInstance.SendMessage('SocialManager', 'VoteCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Messages[0].Data) {
        myUnityInstance.SendMessage('SocialManager', 'VoteCallback', result.Messages[0].Data);
        return result.Messages[0].Data
    }

    return undefined;
}

export async function dumdumSendChat(pid, chatText) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: 'SendChatMessage' }],
        data: chatText
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error) {
        myUnityInstance.SendMessage('SocialManager', 'SendChatMessageCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Messages[0].Data) {
        myUnityInstance.SendMessage('SocialManager', 'SendChatMessageCallback', result.Messages[0].Data);
        return result.Messages[0].Data
    }

    return undefined;
}
