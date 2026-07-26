(() => {
  const clamp = (v,min,max)=>Math.max(min,Math.min(max,v));
  function init(){
    const body = document.body;
    const cursor = document.querySelector('.cursor');
    const progress = document.querySelector('.progress span');
    const portals = [...document.querySelectorAll('.image-portal')];
    const themed = [...document.querySelectorAll('[data-theme]')];
    const parallax = [...document.querySelectorAll('.parallax')];
    const campaign = document.querySelector('.opening-campaign');
    const campaignDepth = [...document.querySelectorAll('.campaign-depth')];
    const campaignShots = [...document.querySelectorAll('.campaign-shot')];
    if(body && !body.dataset.theme) body.dataset.theme='vermilion';
    const updateScroll = () => {
      if(progress){ const max = document.documentElement.scrollHeight - innerHeight; progress.style.transform = `scaleX(${max > 0 ? scrollY / max : 0})`; }
      const mobileLayout = matchMedia('(max-width: 900px)').matches || matchMedia('(pointer: coarse)').matches;
      parallax.forEach(el => {
        if(mobileLayout){ el.style.transform = ''; return; }
        const rect = el.getBoundingClientRect(); const speed = Number(el.dataset.speed || .05); const offset = (innerHeight*.5 - (rect.top + rect.height*.5))*speed; el.style.transform = `translate3d(0,${offset}px,0)`;
      });
    };
    if(!window.__tatakaeInkScroll){ addEventListener('scroll', updateScroll, {passive:true}); window.__tatakaeInkScroll=true; }
    updateScroll();
    if(campaign && campaignDepth.length && !campaign.dataset.inkCampaignBound){
      campaign.dataset.inkCampaignBound = 'true';
      const resetCampaign = () => campaignDepth.forEach(el => { el.style.transform = ''; });
      const moveCampaign = e => {
        const r = campaign.getBoundingClientRect();
        const x = ((e.clientX - r.left) / Math.max(1, r.width) - .5);
        const y = ((e.clientY - r.top) / Math.max(1, r.height) - .5);
        campaignDepth.forEach(el => {
          const d = Number(el.dataset.depth || .1);
          el.style.transform = `translate3d(${(x*d*70).toFixed(2)}px,${(y*d*70).toFixed(2)}px,0)`;
        });
      };
      campaign.addEventListener('pointermove', e => { if(matchMedia('(max-width: 900px)').matches || matchMedia('(pointer: coarse)').matches) return resetCampaign(); moveCampaign(e); }, {passive:true});
      campaign.addEventListener('pointerleave', resetCampaign, {passive:true});
    }
    campaignShots.forEach(shot => {
      if(shot.dataset.inkShotBound) return;
      shot.dataset.inkShotBound = 'true';
      shot.addEventListener('pointerenter', () => shot.classList.add('is-active'), {passive:true});
      shot.addEventListener('pointerleave', () => shot.classList.remove('is-active'), {passive:true});
      shot.addEventListener('pointerdown', () => { shot.classList.add('is-active'); setTimeout(() => shot.classList.remove('is-active'), 1200); }, {passive:true});
    });
    if(cursor && !matchMedia('(pointer: coarse)').matches && !window.__tatakaeInkCursor){
      let mouseX=innerWidth/2, mouseY=innerHeight/2, cx=mouseX, cy=mouseY;
      addEventListener('pointermove', e => { mouseX=e.clientX; mouseY=e.clientY; }, {passive:true});
      const cursorLoop=()=>{ cx+=(mouseX-cx)*.16; cy+=(mouseY-cy)*.16; cursor.style.left=cx+'px'; cursor.style.top=cy+'px'; requestAnimationFrame(cursorLoop); };
      window.__tatakaeInkCursor=true; cursorLoop();
    }
    portals.forEach(portal => {
      if(portal.dataset.inkBound) return; portal.dataset.inkBound='true';
      let x=50,y=50,tx=50,ty=50,rx=0,ry=0,trx=0,tryy=0,radius=0,targetRadius=0,active=false;
      const finePointer = matchMedia('(hover:hover) and (pointer:fine)').matches;
      if(finePointer){
        portal.addEventListener('pointermove', e => { const r=portal.getBoundingClientRect(); tx=clamp(((e.clientX-r.left)/r.width)*100,2,98); ty=clamp(((e.clientY-r.top)/r.height)*100,2,98); trx=(ty-50)*-.055; tryy=(tx-50)*.055; }, {passive:true});
        portal.addEventListener('pointerenter', e => { const r=portal.getBoundingClientRect(); x=tx=clamp(((e.clientX-r.left)/r.width)*100,2,98); y=ty=clamp(((e.clientY-r.top)/r.height)*100,2,98); targetRadius=34; active=true; portal.classList.add('is-active'); body.dataset.theme=portal.dataset.theme || 'vermilion'; if(cursor){ cursor.classList.add('active'); const label=cursor.querySelector('b'); if(label) label.textContent=portal.dataset.label || 'SHIFT'; } });
        portal.addEventListener('pointerleave', () => { targetRadius=0; trx=0; tryy=0; active=false; portal.classList.remove('is-active'); if(cursor){ cursor.classList.remove('active'); const label=cursor.querySelector('b'); if(label) label.textContent='SHIFT'; } });
      } else {
        // Phones do not have real hover. Keep the reveal closed by default
        // so editorial panels do not show a large permanent circle mask.
        targetRadius = 0;
        active = false;
        portal.classList.remove('is-active');
        portal.addEventListener('pointerdown', () => {
          active = true;
          targetRadius = 42;
          portal.classList.add('is-active');
          setTimeout(() => {
            targetRadius = 0;
            active = false;
            portal.classList.remove('is-active');
          }, 950);
        }, {passive:true});
      }
      const loop=()=>{ x+=(tx-x)*(active?.19:.11); y+=(ty-y)*(active?.19:.11); rx+=(trx-rx)*(active?.15:.09); ry+=(tryy-ry)*(active?.15:.09); radius+=(targetRadius-radius)*(active?.18:.12); portal.style.setProperty('--mx', `${x.toFixed(3)}%`); portal.style.setProperty('--my', `${y.toFixed(3)}%`); portal.style.setProperty('--rx', `${rx.toFixed(3)}deg`); portal.style.setProperty('--ry', `${ry.toFixed(3)}deg`); portal.style.setProperty('--portal-radius', `${radius.toFixed(3)}%`); if(document.body.contains(portal)) requestAnimationFrame(loop); };
      loop();
    });
    themed.filter(el => !el.classList.contains('image-portal')).forEach(el => { if(el.dataset.themeBound) return; el.dataset.themeBound='true'; el.addEventListener('pointerenter', () => body.dataset.theme = el.dataset.theme); });
    const sections=[...document.querySelectorAll('[data-accent]')];
    if(window.__tatakaeInkObserver) window.__tatakaeInkObserver.disconnect();
    const observer = new IntersectionObserver(entries => { const active = entries.filter(x=>x.isIntersecting).sort((a,b)=>b.intersectionRatio-a.intersectionRatio)[0]; if(active && !document.querySelector('.image-portal:hover')) body.dataset.theme = active.target.dataset.accent; }, {threshold:[.25,.45,.65]});
    sections.forEach(s=>observer.observe(s)); window.__tatakaeInkObserver=observer;
  }
  window.tatakaeInk = { init };
})();

(function(){
  const key='tatakae.recently.viewed.products.v1';
  const read=()=>{ try { return JSON.parse(localStorage.getItem(key) || '[]'); } catch { return []; } };
  const write=(items)=>{ try { localStorage.setItem(key, JSON.stringify(items.slice(0,12))); } catch { /* ignored */ } };
  window.tatakaeEngagement = window.tatakaeEngagement || {};
  window.tatakaeEngagement.trackViewedProduct = function(product){
    if(!product || !product.id) return;
    const id = String(product.id);
    const current = read().filter(x => String(x.id) !== id);
    current.unshift(product);
    write(current);
  };
  window.tatakaeEngagement.getViewedProductsJson = function(){ return JSON.stringify(read()); };
  window.tatakaeEngagement.clearViewedProducts = function(){ try { localStorage.removeItem(key); } catch { /* ignored */ } };
})();
