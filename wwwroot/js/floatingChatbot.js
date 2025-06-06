// Floating Chatbot Widget for Gemini API (client-side only)
// Paste your API key and system message below
const GEMINI_PROXY_ENDPOINT = "/api/gemini-proxy"; // Sử dụng endpoint proxy mới
const SYSTEM_MESSAGE = `Bạn là trợ lý AI của nền tảng Trọ Tốt, một website giúp người dùng tìm kiếm, đặt phòng trọ, căn hộ, nhà ở và hỗ trợ chủ trọ quản lý phòng, lịch hẹn, yêu cầu thuê. Hãy trả lời ngắn gọn, thân thiện, chính xác, bằng tiếng Việt, sử dụng markdown nếu phù hợp.

# Thông tin về Trọ Tốt
- Trọ Tốt là nền tảng kết nối khách thuê và chủ trọ/phòng/căn hộ.
- Người dùng có thể tìm kiếm phòng, xem chi tiết, đặt lịch hẹn xem phòng, gửi yêu cầu thuê, quản lý các yêu cầu và lịch hẹn.
- Chủ trọ có thể đăng bài cho thuê, quản lý phòng, xem và duyệt các yêu cầu thuê, quản lý lịch hẹn.

# Một số flow cơ bản:
## Đối với khách thuê:
1. **Tìm phòng:** Vào trang "Phòng", lọc theo khu vực, giá, tiện nghi.
2. **Xem chi tiết phòng:** Nhấn vào phòng để xem mô tả, giá, tiện nghi, liên hệ chủ trọ.
3. **Đặt lịch xem phòng:** Nhấn "Đặt lịch xem phòng", chọn thời gian phù hợp.
4. **Gửi yêu cầu thuê:** Nhấn "Yêu cầu đặt phòng", nhập thông tin và gửi.
5. **Quản lý yêu cầu/lịch hẹn:** Vào "Yêu cầu đặt phòng" hoặc "Lịch hẹn" để xem trạng thái.

## Đối với chủ trọ:
1. **Đăng bài cho thuê:** Vào "Tòa nhà của tôi" hoặc "Phòng của tôi", thêm mới phòng/căn hộ.
2. **Quản lý phòng:** Chỉnh sửa, ẩn/hiện, xoá phòng.
3. **Xem và duyệt yêu cầu thuê:** Vào "Bảng điều khiển" hoặc "Yêu cầu đặt phòng", xem chi tiết và duyệt.
4. **Quản lý lịch hẹn:** Vào "Lịch xem phòng" để xem các lịch hẹn với khách.

Nếu người dùng hỏi về các thao tác trên, hãy hướng dẫn chi tiết từng bước bằng tiếng Việt, sử dụng markdown (danh sách, tiêu đề, in đậm, v.v). Nếu không rõ, hãy đề xuất liên hệ bộ phận hỗ trợ.`; // <-- Customize as needed

(function() {
    // --- CSS ---
    const style = document.createElement('style');
    style.innerHTML = `
    #floating-chatbot-btn {
        position: fixed;
        bottom: 24px;
        right: 24px;
        z-index: 9999;
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: #0078d4;
        color: #fff;
        border: none;
        box-shadow: 0 2px 16px rgba(0,0,0,0.18);
        font-size: 28px;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: background 0.2s, box-shadow 0.2s, transform 0.2s;
        outline: none;
        animation: chatbot-fadein 0.5s;
    }
    #floating-chatbot-btn:hover {
        background: #005fa3;
        box-shadow: 0 4px 24px rgba(0,0,0,0.22);
        transform: scale(1.08);
    }
    #floating-chatbot-btn svg {
        width: 32px;
        height: 32px;
        display: block;
    }
    #floating-chatbot-window {
        position: fixed;
        bottom: 90px;
        right: 24px;
        width: 340px;
        max-width: 95vw;
        height: 420px;
        background: #fff;
        border-radius: 18px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.22);
        display: none;
        flex-direction: column;
        z-index: 10000;
        overflow: hidden;
        border: 1px solid #e0e0e0;
        font-family: 'Segoe UI', Arial, sans-serif;
        animation: chatbot-slideup 0.4s;
    }
    @keyframes chatbot-slideup {
        from { transform: translateY(40px); opacity: 0; }
        to { transform: translateY(0); opacity: 1; }
    }
    @keyframes chatbot-fadein {
        from { opacity: 0; }
        to { opacity: 1; }
    }
    #floating-chatbot-header {
        background: #0078d4;
        color: #fff;
        padding: 12px 16px;
        font-weight: bold;
        display: flex;
        align-items: center;
        justify-content: space-between;
        border-bottom: 1px solid #0063a1;
    }
    #floating-chatbot-header .chatbot-avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        background: #fff;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-right: 10px;
        box-shadow: 0 1px 4px rgba(0,0,0,0.08);
    }
    #floating-chatbot-header .chatbot-avatar svg {
        width: 22px;
        height: 22px;
        color: #0078d4;
    }
    #floating-chatbot-header .chatbot-title {
        flex: 1;
        font-size: 17px;
        font-weight: 600;
        margin-left: 2px;
    }
    #floating-chatbot-close {
        background: none;
        border: none;
        color: #fff;
        font-size: 22px;
        cursor: pointer;
        margin-left: 8px;
        transition: color 0.2s;
    }
    #floating-chatbot-close:hover {
        color: #ffb4b4;
    }
    #floating-chatbot-messages {
        flex: 1;
        padding: 16px 12px 12px 12px;
        overflow-y: auto;
        background: #f7fafd;
        font-size: 15px;
        scrollbar-width: thin;
        scrollbar-color: #b3d3f6 #f7fafd;
    }
    #floating-chatbot-messages::-webkit-scrollbar {
        width: 7px;
        background: #f7fafd;
    }
    #floating-chatbot-messages::-webkit-scrollbar-thumb {
        background: #b3d3f6;
        border-radius: 8px;
    }
    .chatbot-msg {
        margin-bottom: 12px;
        display: flex;
        flex-direction: column;
    }
    .chatbot-msg.user {
        align-items: flex-end;
    }
    .chatbot-msg.bot {
        align-items: flex-start;
    }
    .chatbot-bubble {
        padding: 10px 16px;
        border-radius: 18px;
        max-width: 80%;
        word-break: break-word;
        background: #e3f0fc;
        color: #222;
        margin-bottom: 2px;
        box-shadow: 0 1px 4px rgba(0,0,0,0.06);
        font-size: 15px;
        line-height: 1.5;
        transition: background 0.2s;
    }
    .chatbot-msg.user .chatbot-bubble {
        background: #0078d4;
        color: #fff;
        border-bottom-right-radius: 6px;
    }
    .chatbot-msg.bot .chatbot-bubble {
        border-bottom-left-radius: 6px;
    }
    .chatbot-msg.bot .chatbot-bubble {
        background: #e3f0fc;
        color: #222;
    }
    .chatbot-msg .chatbot-bubble:empty::after {
        content: '...';
        color: #aaa;
    }
    #floating-chatbot-input-row {
        display: flex;
        padding: 12px 10px 12px 10px;
        border-top: 1px solid #e0e0e0;
        background: #f4f8fb;
        align-items: center;
    }
    #floating-chatbot-input {
        flex: 1;
        border: 1.5px solid #b3d3f6;
        border-radius: 18px;
        padding: 10px 14px;
        font-size: 15px;
        outline: none;
        background: #fff;
        transition: border 0.2s;
    }
    #floating-chatbot-input:focus {
        border: 1.5px solid #0078d4;
        background: #f7fafd;
    }
    #floating-chatbot-send {
        background: #0078d4;
        color: #fff;
        border: none;
        border-radius: 18px;
        margin-left: 10px;
        padding: 10px 20px;
        font-size: 15px;
        cursor: pointer;
        transition: background 0.2s;
        font-weight: 500;
        box-shadow: 0 1px 4px rgba(0,0,0,0.06);
    }
    #floating-chatbot-send:disabled {
        background: #b3d3f6;
        cursor: not-allowed;
    }
    #floating-chatbot-expand {
        background: none;
        border: none;
        color: #fff;
        font-size: 20px;
        cursor: pointer;
        margin-left: 8px;
        transition: color 0.2s;
    }
    #floating-chatbot-expand:hover {
        color: #b3d3f6;
    }
    #floating-chatbot-modal {
        position: fixed;
        top: 0; left: 0; right: 0; bottom: 0;
        z-index: 10001;
        background: rgba(0,0,0,0.25);
        display: none;
        align-items: center;
        justify-content: center;
        animation: chatbot-fadein 0.3s;
    }
    #floating-chatbot-modal-content {
        background: #fff;
        border-radius: 18px;
        width: 90vw;
        max-width: 700px;
        height: 80vh;
        display: flex;
        flex-direction: column;
        box-shadow: 0 8px 32px rgba(0,0,0,0.22);
        border: 1px solid #e0e0e0;
        font-family: 'Segoe UI', Arial, sans-serif;
        overflow: hidden;
    }
    #floating-chatbot-modal-header {
        background: #0078d4;
        color: #fff;
        padding: 14px 20px;
        font-weight: bold;
        display: flex;
        align-items: center;
        justify-content: space-between;
        border-bottom: 1px solid #0063a1;
    }
    #floating-chatbot-modal-close {
        background: none;
        border: none;
        color: #fff;
        font-size: 24px;
        cursor: pointer;
        margin-left: 8px;
        transition: color 0.2s;
    }
    #floating-chatbot-modal-close:hover {
        color: #ffb4b4;
    }
    #floating-chatbot-modal-messages {
        flex: 1;
        padding: 18px 16px 14px 16px;
        overflow-y: auto;
        background: #f7fafd;
        font-size: 16px;
    }
    #floating-chatbot-modal-input-row {
        display: flex;
        padding: 14px 14px 14px 14px;
        border-top: 1px solid #e0e0e0;
        background: #f4f8fb;
        align-items: center;
    }
    #floating-chatbot-modal-input {
        flex: 1;
        border: 1.5px solid #b3d3f6;
        border-radius: 18px;
        padding: 12px 16px;
        font-size: 16px;
        outline: none;
        background: #fff;
        transition: border 0.2s;
    }
    #floating-chatbot-modal-input:focus {
        border: 1.5px solid #0078d4;
        background: #f7fafd;
    }
    #floating-chatbot-modal-send {
        background: #0078d4;
        color: #fff;
        border: none;
        border-radius: 18px;
        margin-left: 12px;
        padding: 12px 24px;
        font-size: 16px;
        cursor: pointer;
        transition: background 0.2s;
        font-weight: 500;
        box-shadow: 0 1px 4px rgba(0,0,0,0.06);
    }
    #floating-chatbot-modal-send:disabled {
        background: #b3d3f6;
        cursor: not-allowed;
    }
    .chatbot-bubble.typing {
        background: #f0f4f8 !important;
        color: #aaa !important;
        font-style: italic;
        box-shadow: none;
    }
    `;
    document.head.appendChild(style);

    // --- HTML ---
    const btn = document.createElement('button');
    btn.id = 'floating-chatbot-btn';
    btn.title = 'Trò chuyện với trợ lý AI';
    btn.innerHTML = `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="12" cy="12" r="12" fill="#0078d4"/><path d="M7 10.5C7 9.11929 8.11929 8 9.5 8H14.5C15.8807 8 17 9.11929 17 10.5V13.5C17 14.8807 15.8807 16 14.5 16H10.4142C10.149 16 9.89464 16.1054 9.70711 16.2929L8.35355 17.6464C8.15829 17.8417 7.84171 17.8417 7.64645 17.6464C7.45118 17.4512 7.45118 17.1346 7.64645 16.9393L8.29289 16.2929C8.10536 16.1054 8 15.851 8 15.5858V10.5Z" fill="#fff"/></svg>`;
    document.body.appendChild(btn);

    const chatWindow = document.createElement('div');
    chatWindow.id = 'floating-chatbot-window';
    chatWindow.innerHTML = `
        <div id="floating-chatbot-header">
            <span class="chatbot-avatar"><svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="12" cy="12" r="12" fill="#0078d4"/><path d="M7 10.5C7 9.11929 8.11929 8 9.5 8H14.5C15.8807 8 17 9.11929 17 10.5V13.5C17 14.8807 15.8807 16 14.5 16H10.4142C10.149 16 9.89464 16.1054 9.70711 16.2929L8.35355 17.6464C8.15829 17.8417 7.84171 17.8417 7.64645 17.6464C7.45118 17.4512 7.45118 17.1346 7.64645 16.9393L8.29289 16.2929C8.10536 16.1054 8 15.851 8 15.5858V10.5Z" fill="#fff"/></svg></span>
            <span class="chatbot-title">Trợ lý Trọ Tốt</span>
            <button id="floating-chatbot-expand" title="Mở rộng" aria-label="Mở rộng">⤢</button>
            <button id="floating-chatbot-close">×</button>
        </div>
        <div id="floating-chatbot-messages"></div>
        <form id="floating-chatbot-input-row">
            <input id="floating-chatbot-input" type="text" placeholder="Nhập tin nhắn..." autocomplete="off" required />
            <button id="floating-chatbot-send" type="submit">Gửi</button>
        </form>
    `;
    document.body.appendChild(chatWindow);

    // Modal chat lớn
    const modal = document.createElement('div');
    modal.id = 'floating-chatbot-modal';
    modal.innerHTML = `
        <div id="floating-chatbot-modal-content">
            <div id="floating-chatbot-modal-header">
                <span class="chatbot-title">Trợ lý Trọ Tốt</span>
                <button id="floating-chatbot-modal-close" title="Thu nhỏ" aria-label="Thu nhỏ">×</button>
            </div>
            <div id="floating-chatbot-modal-messages"></div>
            <form id="floating-chatbot-modal-input-row">
                <input id="floating-chatbot-modal-input" type="text" placeholder="Nhập tin nhắn..." autocomplete="off" required />
                <button id="floating-chatbot-modal-send" type="submit">Gửi</button>
            </form>
        </div>
    `;
    document.body.appendChild(modal);

    // --- JS Logic ---
    const messagesDiv = chatWindow.querySelector('#floating-chatbot-messages');
    const input = chatWindow.querySelector('#floating-chatbot-input');
    const sendBtn = chatWindow.querySelector('#floating-chatbot-send');
    // Modal elements
    const modalMessagesDiv = modal.querySelector('#floating-chatbot-modal-messages');
    const modalInput = modal.querySelector('#floating-chatbot-modal-input');
    const modalSendBtn = modal.querySelector('#floating-chatbot-modal-send');
    let chatHistory = [];
    let currentBotBubble = null; // Thêm biến này để quản lý bubble bot hiện tại
    let currentModalBotBubble = null;

    // Hiển thị tin nhắn chào mừng khi mở chat lần đầu
    function showWelcomeMessage(targetDiv, isModal) {
        Array.from(targetDiv.querySelectorAll('.chatbot-typing')).forEach(div => div.remove());
        const realMsgCount = Array.from(targetDiv.children).filter(div => !div.classList.contains('chatbot-typing')).length;
        if (realMsgCount === 0) {
            appendMessage('bot',
                '**Xin chào!** 👋\nTôi là trợ lý AI Trọ Tốt.\nBạn cần hỗ trợ gì?\n\nBạn có thể hỏi về:\n- Tìm phòng, đặt lịch xem phòng\n- Gửi yêu cầu thuê\n- Quản lý phòng, lịch hẹn\n\nHãy nhập câu hỏi bên dưới!', true, false, isModal);
        }
    }

    function syncMessagesTo(targetDiv, isModal) {
        targetDiv.innerHTML = '';
        for (const msg of chatHistory) {
            appendMessage(msg.role, msg.text, true, false, isModal, targetDiv);
        }
    }

    async function sendMessage(text, isModal) {
        appendMessage('user', text, true, false, isModal);
        chatHistory.push({ role: 'user', text });
        Array.from((isModal ? modalMessagesDiv : messagesDiv).querySelectorAll('.chatbot-typing')).forEach(div => div.remove());
        (isModal ? modalSendBtn : sendBtn).disabled = true;
        (isModal ? modalInput : input).disabled = true;
        if (isModal) {
            currentModalBotBubble = appendMessage('bot', '', true, false, true);
        } else {
            currentBotBubble = appendMessage('bot', '', true, false, false);
        }
        const url = GEMINI_PROXY_ENDPOINT;
        const body = {
            system_instruction: {
                parts: [ { text: SYSTEM_MESSAGE } ]
            },
            contents: buildMessages(),
            safetySettings: [
                { category: "HARM_CATEGORY_DANGEROUS_CONTENT", threshold: "BLOCK_ONLY_HIGH" }
            ],
            generationConfig: {
                stopSequences: ["Title"],
                temperature: 1,
                maxOutputTokens: 800,
                topP: 0.8,
                topK: 10
            }
        };
        let botMsg = '';
        try {
            const resp = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (!resp.body) throw new Error('No response body');
            const reader = resp.body.getReader();
            let decoder = new TextDecoder();
            let buffer = '';
            while (true) {
                const { value, done } = await reader.read();
                if (done) break;
                buffer += decoder.decode(value, { stream: true });
                let lines = buffer.split('\n');
                buffer = lines.pop();
                for (let line of lines) {
                    if (line.startsWith('data:')) {
                        let data = line.slice(5).trim();
                        if (data && data !== '[DONE]') {
                            try {
                                let json = JSON.parse(data);
                                let part = json.candidates?.[0]?.content?.parts?.[0]?.text;
                                if (part) {
                                    botMsg += part;
                                    setBotTyping(false, isModal);
                                    if (isModal && currentModalBotBubble) {
                                        if (window.marked) {
                                            currentModalBotBubble.innerHTML = window.marked.parse(botMsg);
                                        } else {
                                            currentModalBotBubble.innerHTML = botMsg.replace(/\n/g, '<br>');
                                        }
                                        modalMessagesDiv.scrollTop = modalMessagesDiv.scrollHeight;
                                    } else if (!isModal && currentBotBubble) {
                                        if (window.marked) {
                                            currentBotBubble.innerHTML = window.marked.parse(botMsg);
                                        } else {
                                            currentBotBubble.innerHTML = botMsg.replace(/\n/g, '<br>');
                                        }
                                        messagesDiv.scrollTop = messagesDiv.scrollHeight;
                                    }
                                }
                            } catch {}
                        }
                    }
                }
            }
        } catch (e) {
            setBotTyping(false, isModal);
            if (isModal && currentModalBotBubble) currentModalBotBubble.innerHTML = 'Xin lỗi, đã xảy ra lỗi.';
            if (!isModal && currentBotBubble) currentBotBubble.innerHTML = 'Xin lỗi, đã xảy ra lỗi.';
        }
        setBotTyping(false, isModal);
        if (botMsg) chatHistory.push({ role: 'bot', text: botMsg });
        (isModal ? modalSendBtn : sendBtn).disabled = false;
        (isModal ? modalInput : input).disabled = false;
        (isModal ? modalInput : input).value = '';
        // Chỉ auto focus nếu không phải mobile
        const isMobile = /android|iphone|ipad|ipod|opera mini|iemobile|mobile/i.test(navigator.userAgent) || window.innerWidth < 600;
        if (!isMobile) {
            (isModal ? modalInput : input).focus();
        }
        if (isModal) currentModalBotBubble = null;
        else currentBotBubble = null;
    }

    function appendMessage(role, text, forceNew, isTypingBubble, isModal, targetDiv) {
        const div = targetDiv || (isModal ? modalMessagesDiv : messagesDiv);
        const msgDiv = document.createElement('div');
        msgDiv.className = 'chatbot-msg ' + (role === 'user' ? 'user' : 'bot') + (isTypingBubble ? ' chatbot-typing' : '');
        const bubble = document.createElement('div');
        bubble.className = 'chatbot-bubble' + (isTypingBubble ? ' typing' : '');
        if (role === 'bot' && !isTypingBubble) {
            if (window.marked) {
                bubble.innerHTML = window.marked.parse(text || '');
            } else {
                bubble.innerHTML = (text || '').replace(/\n/g, '<br>');
            }
        } else if (isTypingBubble) {
            bubble.textContent = '...';
        } else {
            bubble.textContent = text;
        }
        msgDiv.appendChild(bubble);
        div.appendChild(msgDiv);
        div.scrollTop = div.scrollHeight;
        return bubble;
    }

    function setBotTyping(isTyping, isModal) {
        const div = isModal ? modalMessagesDiv : messagesDiv;
        if (isTyping && ((isModal && currentModalBotBubble) || (!isModal && currentBotBubble))) return;
        Array.from(div.querySelectorAll('.chatbot-typing')).forEach(d => d.remove());
        if (isTyping) {
            appendMessage('bot', '', true, true, isModal);
        }
    }

    function buildMessages() {
        return chatHistory.map(m => ({
            role: m.role === 'user' ? 'user' : 'model',
            parts: [{ text: m.text }]
        }));
    }

    // Add basic markdown CSS for bot bubbles (only add once)
    if (!document.getElementById('chatbot-md-style')) {
        const mdStyle = document.createElement('style');
        mdStyle.id = 'chatbot-md-style';
        mdStyle.innerHTML = `
        .chatbot-bubble h1, .chatbot-bubble h2, .chatbot-bubble h3 {
            font-size: 1.1em; margin: 0.5em 0 0.2em 0; font-weight: bold;
        }
        .chatbot-bubble ul, .chatbot-bubble ol { margin: 0.3em 0 0.3em 1.2em; }
        .chatbot-bubble li { margin-bottom: 0.2em; }
        .chatbot-bubble pre, .chatbot-bubble code {
            background: #f3f3f3; color: #c7254e; border-radius: 4px; padding: 2px 6px; font-size: 0.97em;
        }
        .chatbot-bubble a { color: #0078d4; text-decoration: underline; }
        .chatbot-bubble blockquote { border-left: 3px solid #b3d3f6; margin: 0.5em 0; padding: 0.2em 0.8em; color: #555; background: #f7fafd; }
        `;
        document.head.appendChild(mdStyle);
    }

    // Load marked.js for markdown rendering if not present
    if (!window.marked) {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/marked/marked.min.js';
        script.onload = function() {
            // Optionally re-render last bot message if needed
        };
        document.head.appendChild(script);
    }

    // --- Event Listeners ---
    btn.onclick = () => {
        chatWindow.style.display = 'flex';
        btn.style.display = 'none';
        syncMessagesTo(messagesDiv, false);
        showWelcomeMessage(messagesDiv, false);
        input.focus();
    };
    chatWindow.querySelector('#floating-chatbot-close').onclick = () => {
        chatWindow.style.display = 'none';
        btn.style.display = 'flex';
    };
    chatWindow.querySelector('#floating-chatbot-expand').onclick = () => {
        chatWindow.style.display = 'none';
        modal.style.display = 'flex';
        syncMessagesTo(modalMessagesDiv, true);
        showWelcomeMessage(modalMessagesDiv, true);
        modalInput.focus();
    };
    chatWindow.querySelector('#floating-chatbot-input-row').onsubmit = (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (text) sendMessage(text, false);
    };
    modal.querySelector('#floating-chatbot-modal-close').onclick = () => {
        modal.style.display = 'none';
        chatWindow.style.display = 'flex';
        syncMessagesTo(messagesDiv, false);
        showWelcomeMessage(messagesDiv, false);
        input.focus();
    };
    modal.querySelector('#floating-chatbot-modal-input-row').onsubmit = (e) => {
        e.preventDefault();
        const text = modalInput.value.trim();
        if (text) sendMessage(text, true);
    };
})();
