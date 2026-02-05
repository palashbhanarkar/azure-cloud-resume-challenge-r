/*==================== MENU SHOW Y HIDDEN ====================*/
const navMenu = document.getElementById('nav-menu'),
      navToggle = document.getElementById('nav-toggle'),
      navClose = document.getElementById('nav-close')

/*===== MENU SHOW =====*/
/* Validate if constant exists */
if(navToggle){
    navToggle.addEventListener('click', () => {
        navMenu.classList.add('show-menu')
    })
}

/*===== MENU HIDDEN =====*/
/* Validate if constant exists */
if(navClose){
    navClose.addEventListener('click', () => {
        navMenu.classList.remove('show-menu')
    })
}

/*==================== REMOVE MENU MOBILE ====================*/
const navLink = document.querySelectorAll('.nav__link')

function linkAction(){
    const navMenu = document.getElementById('nav-menu')
    // When we click on each nav__link, we remove the show-menu class
    navMenu.classList.remove('show-menu')
}
navLink.forEach(n => n.addEventListener('click', linkAction))

/*==================== ACCORDION SKILLS ====================*/
const skillsContent = document.getElementsByClassName('skills__content'),
      skillsHeader = document.querySelectorAll('.skills__header')

function toggleSkills(){
    let itemClass = this.parentNode.className

    for(let i=0; i < skillsContent.length; i++){
        skillsContent[i].className = 'skills__content skills__close'
    }

    if(itemClass === 'skills__content skills__close'){
        this.parentNode.className = 'skills__content skills__open'
    }
}

skillsHeader.forEach((el) => {
    el.addEventListener('click', toggleSkills)
})

/*==================== QUALIFICATION TABS ====================*/


/*==================== PROJECTS MODAL ====================*/
const modalViews = document.querySelectorAll('.projects__modal'),
      modalBtns = document.querySelectorAll('.projects__button'),
      modalCloses = document.querySelectorAll('.projects__modal-close')

let modal = function(modalClick){
    modalViews[modalClick].classList.add('active-modal')
}

modalBtns.forEach((modalBtn, i) => {
    modalBtn.addEventListener('click', () => {
        modal(i)
    })
})

modalCloses.forEach((modalClose) => {
    modalClose.addEventListener('click', () => {
        modalViews.forEach((modalView) => {
            modalView.classList.remove('active-modal')
        })
    })
})

/*==================== CERTIFICATE SWIPER  ====================*/
let swiper = new Swiper('.certificate__container', {
    cssMode: true,
    loop: true,
    navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev',
    },
    pagination: {
        el: '.swiper-pagination',
        clickable: true,
    },
});

/*==================== SCROLL SECTIONS ACTIVE LINK ====================*/
const sections = document.querySelectorAll('section[id]')

function scrollActive(){
    const scrollY = window.pageYOffset

    sections.forEach(current => {
        const sectionHeight = current.offsetHeight
        const sectionTop = current.offsetTop - 50;
        const sectionId = current.getAttribute('id')

        if(scrollY > sectionTop && scrollY <= sectionTop + sectionHeight){
            document.querySelector('.nav__menu a[href*=' + sectionId + ']').classList.add('active-link')
        } else {
            document.querySelector('.nav__menu a[href*=' + sectionId + ']').classList.remove('active-link')
        }   
    })
}

/*==================== CHANGE BACKGROUND HEADER ====================*/ 


/*==================== SHOW SCROLL UP ====================*/ 
function scrollUp(){
    const scrollUp = document.getElementById('scroll-up');
    
    if(this.scrollY >= 560) scrollUp.classList.add('show-scroll'); else scrollUp.classList.remove('show-scroll')
}
window.addEventListener('scroll', scrollUp)

/*==================== VISITOR COUNTER ====================*/

// Configuration - move to environment variable in production
const VISITOR_COUNTER_CONFIG = {
    apiUrl: window.VISITOR_COUNTER_API_URL || "/api/VisitorCounter", //updated to match API route
    counterId: "counter",
    timeout: 5000, // 5 seconds
    storageKey: "resume_visitor_counted" // localStorage key to track if this browser is a counted visitor
};

/**
 * Check if this browser has already been counted as a visitor
 * @returns {boolean} true if visitor has already been counted, false otherwise
 */
const isVisitorAlreadyCounted = () => {
    try {
        return localStorage.getItem(VISITOR_COUNTER_CONFIG.storageKey) === 'true';
    } catch (error) {
        console.warn("localStorage not available:", error.message);
        return false;
    }
};

/**
 * Mark this browser as a counted visitor in localStorage
 */
const markVisitorCounted = () => {
    try {
        localStorage.setItem(VISITOR_COUNTER_CONFIG.storageKey, 'true');
        console.log("Visitor marked as counted in localStorage");
    } catch (error) {
        console.warn("Failed to mark visitor in localStorage:", error.message);
    }
};

/**
 * Fetch the current visitor count from the API and increment if new visitor
 */
const getVisitorCount = async () => {
    const counterElement = document.getElementById(VISITOR_COUNTER_CONFIG.counterId);
    
    if (!counterElement) {
        console.warn("Counter element not found in DOM");
        return null;
    }

    // Debug logging
    console.log("✓ Counter element found:", counterElement);
    console.log("  - Tag:", counterElement.tagName);
    console.log("  - ID:", counterElement.id);
    console.log("  - Parent:", counterElement.parentElement?.className);
    console.log("  - Visible (offsetHeight > 0):", counterElement.offsetHeight > 0);
    console.log("  - Display CSS:", window.getComputedStyle(counterElement).display);
    console.log("  - Current text:", counterElement.textContent);

    // Set initial loading state
    counterElement.textContent = "Loading...";

    try {
        // Check if this visitor has already been counted
        const alreadyCounted = isVisitorAlreadyCounted();
        
        if (!alreadyCounted) {
            // New visitor: Call API to increment counter
            console.log("New visitor detected. Calling API to increment counter...");
            
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), VISITOR_COUNTER_CONFIG.timeout);

            const response = await fetch(VISITOR_COUNTER_CONFIG.apiUrl, {
                method: 'GET',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (!response.ok) {
                throw new Error(`API returned ${response.status}: ${response.statusText}`);
            }

            const visitorCount = await response.text();
            
            if (!visitorCount || isNaN(visitorCount)) {
                throw new Error(`Invalid counter value received: ${visitorCount}`);
            }

            // Mark this browser as counted
            markVisitorCounted();

            // Update DOM
            counterElement.textContent = visitorCount;
            console.log(`Visitor count incremented to: ${visitorCount}`);
            
            return visitorCount;
        } else {
            // Returning visitor: Fetch count without incrementing
            console.log("Returning visitor detected. Fetching current count (no increment)...");
            
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), VISITOR_COUNTER_CONFIG.timeout);

            const response = await fetch(VISITOR_COUNTER_CONFIG.apiUrl, {
                method: 'GET',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (!response.ok) {
                throw new Error(`API returned ${response.status}: ${response.statusText}`);
            }

            const visitorCount = await response.text();
            
            if (!visitorCount || isNaN(visitorCount)) {
                throw new Error(`Invalid counter value received: ${visitorCount}`);
            }

            // Update DOM
            counterElement.textContent = visitorCount;
            console.log(`Visitor count displayed (no increment): ${visitorCount}`);
            
            return visitorCount;
        }
    } catch (error) {
        if (error.name === 'AbortError') {
            console.error("Visitor counter API request timed out");
        } else {
            console.error(`Failed to fetch visitor count: ${error.message}`);
        }
        
        // Set fallback display
        counterElement.textContent = "N/A";
        counterElement.setAttribute('title', 'Counter temporarily unavailable');
        
        return null;
    }
};

// Fetch visitor count when DOM is ready
window.addEventListener('DOMContentLoaded', (event) => {
    getVisitorCount();
})
