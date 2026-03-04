const API_URL = "http://localhost:5228/api"; 

function getRarityClass(weight) {
    if (weight >= 50) return "text-common";      
    if (weight >= 20) return "text-uncommon";    
    if (weight >= 10) return "text-rare";        
    if (weight >= 5) return "text-epic";         
    if (weight > 1) return "text-legendary";     
    return "text-mythic";                        
}

async function updatePlayerHeader(player) {
    document.getElementById('playerName').innerText = player.username;
    document.getElementById('playerGold').innerText = player.gold;
    
    const clanSpan = document.getElementById('playerClan');
    
    if (player.clanId) {
        try {
            const clanResp = await fetch(`${API_URL}/clan/all`);
            const clans = await clanResp.json();
            const myClan = clans.find(c => c.id === player.clanId);
            
            if (myClan) {
                clanSpan.innerHTML = `<span class="badge bg-success shadow-sm">🛡️ ${myClan.name}</span>`;
            }
        } catch {
            clanSpan.innerHTML = "";
        }
    } else {
         clanSpan.innerHTML = `<span class="badge bg-secondary shadow-sm">Bez klana</span>`;
    }
}

async function register() {
    const username = document.getElementById('regUsername').value.trim();
    const messageDiv = document.getElementById('regMessage');
    
    if (!username) {
        messageDiv.innerHTML = `<span class="text-warning">Korisničko ime je obavezno!</span>`;
        return;
    }

    const newPlayer = {
        username: username,
        gold: 100,       
        inventory: []    
    };

    try {
        messageDiv.innerHTML = `<span class="text-light">Kreiranje naloga...</span>`;
        
        const response = await fetch(`${API_URL}/player/create`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(newPlayer)
        });

        const data = await response.json();

        if (response.ok) {
            messageDiv.innerHTML = `<span class="text-success">Nalog uspešno kreiran! Preusmeravanje...</span>`;
            setTimeout(() => {
                window.location.href = "index.html";
            }, 1500);
        } else {
            messageDiv.innerHTML = `<span class="text-danger">Greška: ${data.error}</span>`;
        }
    } catch (error) {
        messageDiv.innerHTML = `<span class="text-danger">Greška u komunikaciji sa serverom.</span>`;
    }
}

async function login() {
    const username = document.getElementById('username').value;
    const messageDiv = document.getElementById('message');
    if (!username) return;

    try {
        const response = await fetch(`${API_URL}/player/login?username=${username}`, { method: 'POST' });
        const data = await response.json();

        if (response.ok) {
            sessionStorage.setItem('sessionId', data.sessionId);
            window.location.href = "dashboard.html"; 
        } else {
            messageDiv.innerHTML = `<span class="text-danger">${data.error}</span>`;
        }
    } catch (error) {
        messageDiv.innerHTML = `<span class="text-danger">Greška: Proverite da li je API pokrenut.</span>`;
    }
}

function logout() {
    sessionStorage.removeItem('sessionId');
    window.location.href = "index.html";
}

async function loadDashboard() {
    const sessionId = sessionStorage.getItem('sessionId');
    if (!sessionId) { window.location.href = "index.html"; return; }

    loadLootBoxes();

    try {
        const catalogResponse = await fetch(`${API_URL}/item/all`);
        const catalogItems = await catalogResponse.json();
        const itemWeights = {};
        catalogItems.forEach(i => itemWeights[i.itemId] = i.dropWeight);

        const response = await fetch(`${API_URL}/player/${sessionId}`);
        const player = await response.json();

        if (response.ok) {
            await updatePlayerHeader(player); 

            const inventoryContainer = document.getElementById('inventoryContainer');
            inventoryContainer.innerHTML = ""; 

            if (player.inventory.length === 0) {
                inventoryContainer.innerHTML = "<p class='text-muted'>Vaš ranac je prazan.</p>";
            } else {
                player.inventory.forEach(item => {
                    const rarityClass = getRarityClass(itemWeights[item.itemId] || 50);
                    inventoryContainer.innerHTML += `
                        <div class="col-md-4 mb-3">
                            <div class="card bg-dark border-secondary text-center p-3 h-100 shadow">
                                <h6 class="${rarityClass} fw-bold">${item.name}</h6>
                                <p class="small text-muted mb-1" style="font-size: 0.7rem;">ID: ${item.itemId}</p>
                                <span class="badge bg-secondary text-light">Količina: ${item.quantity}</span>
                            </div>
                        </div>
                    `;
                });
            }
        }
    } catch (error) {
        console.error(error);
    }
}

async function loadLootBoxes() {
    try {
        const response = await fetch(`${API_URL}/lootbox/all`);
        const boxes = await response.json();
        const container = document.getElementById('availableBoxesContainer');
        container.innerHTML = "";
        
        boxes.forEach(box => {
            container.innerHTML += `
                <div class="border border-secondary rounded p-2 mb-2 bg-secondary shadow-sm">
                    <h6 class="text-info fw-bold mb-1">${box.name}</h6>
                    <p class="small text-light mb-2">Šansa za: <span class="badge bg-dark border border-info">${box.targetItemType}</span></p>
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="small text-warning" style="font-size: 0.75rem;">${box.boxId}</span>
                        <button class="btn btn-sm btn-outline-warning" onclick="selectBox('${box.boxId}')">Izaberi</button>
                    </div>
                </div>
            `;
        });
    } catch (error) {}
}

function selectBox(boxId) { document.getElementById('boxIdInput').value = boxId; }

async function openLootBox() {
    const boxId = document.getElementById('boxIdInput').value;
    const sessionId = sessionStorage.getItem('sessionId');
    const msgDiv = document.getElementById('lootboxMessage');

    try {
        msgDiv.innerHTML = `<span class="text-light">Sreća prati hrabre... 🎰</span>`;
        const response = await fetch(`${API_URL}/player/${sessionId}/lootbox/${boxId}`, { method: 'POST' });
        
        let resultText = "";
        try {
            const resultJson = await response.json();
            resultText = resultJson.message || resultJson.error;
        } catch { resultText = await response.text(); }

        if (response.ok) {
            msgDiv.innerHTML = `<span class="text-success">🎉 ${resultText}</span>`;
            loadDashboard(); 
        } else {
            msgDiv.innerHTML = `<span class="text-danger">❌ ${resultText}</span>`;
        }
    } catch (error) {}
}

async function loadAuctionPage() {
    const sessionId = sessionStorage.getItem('sessionId');
    if (!sessionId) { window.location.href = "index.html"; return; }

    try {
        const playerResponse = await fetch(`${API_URL}/player/${sessionId}`);
        const player = await playerResponse.json();

        if (playerResponse.ok) {
            await updatePlayerHeader(player); 

            const selectElement = document.getElementById('auctionItemSelect');
            selectElement.innerHTML = "";
            if (player.inventory.length === 0) {
                selectElement.innerHTML = `<option value="">Ranac je prazan</option>`;
                selectElement.disabled = true;
            } else {
                selectElement.disabled = false;
                player.inventory.forEach(item => {
                    selectElement.innerHTML += `<option value="${item.itemId}">${item.name} (Max: ${item.quantity})</option>`;
                });
            }
        }
        await fetchActiveAuctions();
    } catch (error) {}
}

async function fetchActiveAuctions() {
    const sessionId = sessionStorage.getItem('sessionId');
    try {
        const response = await fetch(`${API_URL}/auction/active`);
        const auctions = await response.json();
        const container = document.getElementById('activeAuctionsContainer');
        container.innerHTML = "";

        if (auctions.length === 0) {
            container.innerHTML = `<p class="text-muted">Trenutno nema aktivnih aukcija.</p>`;
            return;
        }

        auctions.forEach(auction => {
            const isMyAuction = auction.sellerId === sessionId;
            const myHighestBid = auction.highestBidderId === sessionId;

            let statusBadge = `<span class="badge bg-secondary">Nema ponuda</span>`;
            if (myHighestBid) statusBadge = `<span class="badge bg-success">Vi vodite!</span>`;
            else if (auction.highestBidderId) statusBadge = `<span class="badge bg-warning text-dark">Licitirano</span>`;

            container.innerHTML += `
                <div class="col-md-6 mb-3">
                    <div class="card bg-dark border-secondary shadow h-100">
                        <div class="card-header border-secondary d-flex justify-content-between align-items-center">
                            <span class="text-info fw-bold">${auction.item.name} x${auction.item.quantity}</span>
                            ${statusBadge}
                        </div>
                        <div class="card-body p-3">
                            <h4 class="text-warning mb-3"><span class="coin-icon"></span> ${auction.currentBid}</h4>
                            ${isMyAuction ? 
                                `<p class="text-success small fw-bold">Ovo je vaša aukcija.</p>` : 
                                `
                                <div class="input-group input-group-sm">
                                    <input type="number" id="bidAmount_${auction.id}" class="form-control bg-secondary text-white border-warning" min="${auction.currentBid + 1}" value="${auction.currentBid + 1}">
                                    <button class="btn btn-warning fw-bold" onclick="placeBid('${auction.id}')">Licitiraj</button>
                                </div>
                                `
                            }
                        </div>
                    </div>
                </div>
            `;
        });
    } catch (error) {}
}

async function createAuction() {
    const sessionId = sessionStorage.getItem('sessionId');
    const itemId = document.getElementById('auctionItemSelect').value;
    const quantity = document.getElementById('auctionQuantity').value;
    const price = document.getElementById('auctionPrice').value;
    const duration = document.getElementById('auctionDuration').value;

    const url = `${API_URL}/auction/create?playerId=${sessionId}&itemId=${itemId}&quantity=${quantity}&startingPrice=${price}&durationInHours=${duration}`;
    await fetch(url, { method: 'POST' });
    loadAuctionPage();
}

async function placeBid(auctionId) {
    const sessionId = sessionStorage.getItem('sessionId');
    const bidAmount = document.getElementById(`bidAmount_${auctionId}`).value;
    const url = `${API_URL}/auction/bid?auctionId=${auctionId}&playerId=${sessionId}&bidAmount=${bidAmount}`;
    await fetch(url, { method: 'POST' });
    loadAuctionPage();
}

async function loadClanPage() {
    const sessionId = sessionStorage.getItem('sessionId');
    if (!sessionId) { window.location.href = "index.html"; return; }

    try {
        const playerResponse = await fetch(`${API_URL}/player/${sessionId}`);
        const player = await playerResponse.json();

        if (playerResponse.ok) {
            await updatePlayerHeader(player);
        }

        await fetchLeaderboard();
        await fetchAvailableClans(player.clanId);

    } catch (error) {}
}

async function fetchLeaderboard() {
    try {
        const response = await fetch(`${API_URL}/clan/leaderboard`);
        const clans = await response.json();
        const tbody = document.getElementById('leaderboardBody');
        tbody.innerHTML = "";

        if(clans.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Baza klanova je prazna.</td></tr>`;
            return;
        }

        clans.forEach((clan, index) => {
            let medal = index + 1;
            if (index === 0) medal = "🥇";
            if (index === 1) medal = "🥈";
            if (index === 2) medal = "🥉";

            tbody.innerHTML += `
                <tr>
                    <td class="text-center fw-bold fs-5">${medal}</td>
                    <td class="text-white fw-bold align-middle" style="letter-spacing: 0.5px;">${clan.clanName}</td>
                    <td class="text-center align-middle">${clan.members}</td>
                    <td class="text-warning fw-bold text-end align-middle"><span class="coin-icon"></span> ${clan.totalGold}</td>
                </tr>
            `;
        });
    } catch (e) { console.error(e); }
}

async function fetchAvailableClans(playerClanId) {
    try {
        const response = await fetch(`${API_URL}/clan/all`);
        const clans = await response.json();
        const container = document.getElementById('availableClansContainer');
        container.innerHTML = "";

        clans.forEach(clan => {
            const isMyClan = clan.id === playerClanId;
            
            container.innerHTML += `
                <div class="border border-secondary rounded p-3 mb-2 bg-secondary shadow-sm">
                    <h5 class="text-success fw-bold mb-1">${clan.name}</h5>
                    <p class="small text-light mb-3">${clan.description}</p>
                    
                    ${isMyClan ? 
                        `<span class="badge bg-success w-100 py-2">Vaš trenutni klan</span>` : 
                        `<button class="btn btn-sm btn-outline-success w-100 fw-bold" onclick="joinClan('${clan.id}')">Pridruži se</button>`
                    }
                </div>
            `;
        });
    } catch (e) { console.error(e); }
}

async function joinClan(clanId) {
    const sessionId = sessionStorage.getItem('sessionId');
    try {
        const response = await fetch(`${API_URL}/clan/join?playerId=${sessionId}&clanId=${clanId}`, { method: 'POST' });
        if (response.ok) {
            loadClanPage();
        } else {
            const data = await response.json();
            alert("Greška: " + data.error);
        }
    } catch (error) {
        alert("Greška u komunikaciji sa serverom.");
    }
}