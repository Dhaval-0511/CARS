// API Base URL
const API_BASE_URL = 'https://localhost:7001/api/v1';

// State Management
let currentUser = null;
let authToken = null;

// Initialize App
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    setupEventListeners();
});

function setupEventListeners() {
    document.getElementById('btnLogout')?.addEventListener('click', handleLogout);
}

function checkAuth() {
    const token = localStorage.getItem('authToken');
    const user = localStorage.getItem('currentUser');

    if (token && user) {
        authToken = token;
        currentUser = JSON.parse(user);
        showDashboard();
    } else {
        showAuth();
    }
}

// Tab Switching
function showTab(tabName) {
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');
    const tabs = document.querySelectorAll('.tab-btn');

    tabs.forEach(tab => tab.classList.remove('active'));

    if (tabName === 'login') {
        loginForm.style.display = 'block';
        registerForm.style.display = 'none';
        tabs[0].classList.add('active');
    } else {
        loginForm.style.display = 'none';
        registerForm.style.display = 'block';
        tabs[1].classList.add('active');
    }
}

// Authentication Functions
async function handleLogin(event) {
    event.preventDefault();

    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        const result = await response.json();

        if (result.success) {
            authToken = result.data.token;
            currentUser = result.data.user;
            
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
            
            showToast('Login successful!', 'success');
            showDashboard();
        } else {
            showToast(result.message || 'Login failed', 'error');
        }
    } catch (error) {
        console.error('Login error:', error);
        showToast('Network error. Please check if the API is running.', 'error');
    }
}

async function handleRegister(event) {
    event.preventDefault();

    const fullName = document.getElementById('regFullName').value;
    const email = document.getElementById('regEmail').value;
    const phoneNumber = document.getElementById('regPhone').value;
    const password = document.getElementById('regPassword').value;

    try {
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ fullName, email, phoneNumber, password })
        });

        const result = await response.json();

        if (result.success) {
            authToken = result.data.token;
            currentUser = result.data.user;
            
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
            
            showToast('Registration successful!', 'success');
            showDashboard();
        } else {
            showToast(result.message || 'Registration failed', 'error');
        }
    } catch (error) {
        console.error('Registration error:', error);
        showToast('Network error. Please check if the API is running.', 'error');
    }
}

function handleLogout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    authToken = null;
    currentUser = null;
    showToast('Logged out successfully', 'success');
    showAuth();
}

// UI Navigation
function showAuth() {
    document.getElementById('authSection').style.display = 'block';
    document.getElementById('dashboardSection').style.display = 'none';
    document.getElementById('btnLogout').style.display = 'none';
}

function showDashboard() {
    document.getElementById('authSection').style.display = 'none';
    document.getElementById('dashboardSection').style.display = 'block';
    document.getElementById('btnLogout').style.display = 'block';
    
    document.getElementById('userName').textContent = currentUser.fullName;
    document.getElementById('userRole').textContent = currentUser.role;

    loadDoctors();
    loadAppointments();
}

// Load Doctors
async function loadDoctors() {
    try {
        const response = await fetch(`${API_BASE_URL}/doctors`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        const result = await response.json();

        if (result.success && result.data) {
            displayDoctors(result.data);
        }
    } catch (error) {
        console.error('Error loading doctors:', error);
    }
}

function displayDoctors(doctors) {
    const container = document.getElementById('doctorsList');
    
    if (doctors.length === 0) {
        container.innerHTML = '<p>No doctors available at the moment.</p>';
        return;
    }

    container.innerHTML = doctors.map(doctor => `
        <div class="doctor-card">
            <h4>Dr. ${doctor.fullName}</h4>
            <p class="specialization">${doctor.specialization}</p>
            <p class="bio">${doctor.bio || 'No bio available'}</p>
            <p style="margin-top: 0.5rem; color: ${doctor.isAvailable ? '#10b981' : '#ef4444'}; font-weight: 600;">
                ${doctor.isAvailable ? '✓ Available' : '✗ Not Available'}
            </p>
        </div>
    `).join('');
}

// Load Appointments
async function loadAppointments() {
    try {
        const response = await fetch(`${API_BASE_URL}/appointments/my`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        const result = await response.json();

        if (result.success && result.data) {
            displayAppointments(result.data);
        }
    } catch (error) {
        console.error('Error loading appointments:', error);
    }
}

function displayAppointments(appointments) {
    const container = document.getElementById('appointmentsList');
    
    if (appointments.length === 0) {
        container.innerHTML = '<p style="margin-top: 1rem; color: #64748b;">No appointments found. Book your first appointment!</p>';
        return;
    }

    container.innerHTML = appointments.map(appointment => {
        const date = new Date(appointment.appointmentDate);
        const status = appointment.status.toLowerCase();
        
        return `
            <div class="appointment-card">
                <div class="appointment-info">
                    <h4>Dr. ${appointment.doctorName}</h4>
                    <p>📅 ${date.toLocaleDateString()} at ${date.toLocaleTimeString()}</p>
                    <p>📝 ${appointment.notes || 'No notes'}</p>
                </div>
                <div>
                    <div class="appointment-status status-${status}">
                        ${appointment.status}
                    </div>
                    ${status === 'pending' ? `
                        <button class="btn-danger" style="margin-top: 0.5rem;" onclick="cancelAppointment('${appointment.id}')">
                            Cancel
                        </button>
                    ` : ''}
                </div>
            </div>
        `;
    }).join('');
}

// Book Appointment
function showBookAppointment() {
    loadDoctorsForBooking();
    document.getElementById('appointmentModal').style.display = 'block';
}

async function loadDoctorsForBooking() {
    try {
        const response = await fetch(`${API_BASE_URL}/doctors`);
        const result = await response.json();

        if (result.success && result.data) {
            const select = document.getElementById('appointmentDoctor');
            select.innerHTML = '<option value="">Select a doctor</option>' +
                result.data.filter(d => d.isAvailable).map(doctor => 
                    `<option value="${doctor.id}">Dr. ${doctor.fullName} - ${doctor.specialization}</option>`
                ).join('');
        }
    } catch (error) {
        console.error('Error loading doctors:', error);
    }
}

async function handleBookAppointment(event) {
    event.preventDefault();

    const doctorId = document.getElementById('appointmentDoctor').value;
    const appointmentDate = document.getElementById('appointmentDate').value;
    const notes = document.getElementById('appointmentNotes').value;

    try {
        const response = await fetch(`${API_BASE_URL}/appointments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({
                doctorId,
                appointmentDate,
                notes
            })
        });

        const result = await response.json();

        if (result.success) {
            showToast('Appointment booked successfully!', 'success');
            closeModal();
            loadAppointments();
        } else {
            showToast(result.message || 'Failed to book appointment', 'error');
        }
    } catch (error) {
        console.error('Error booking appointment:', error);
        showToast('Network error', 'error');
    }
}

async function cancelAppointment(appointmentId) {
    if (!confirm('Are you sure you want to cancel this appointment?')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/appointments/${appointmentId}/cancel`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        const result = await response.json();

        if (result.success) {
            showToast('Appointment cancelled successfully', 'success');
            loadAppointments();
        } else {
            showToast('Failed to cancel appointment', 'error');
        }
    } catch (error) {
        console.error('Error cancelling appointment:', error);
        showToast('Network error', 'error');
    }
}

function closeModal() {
    document.getElementById('appointmentModal').style.display = 'none';
    document.getElementById('appointmentDoctor').value = '';
    document.getElementById('appointmentDate').value = '';
    document.getElementById('appointmentNotes').value = '';
}

// Toast Notifications
function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = `toast show ${type}`;
    
    setTimeout(() => {
        toast.className = 'toast';
    }, 3000);
}

// Close modal when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('appointmentModal');
    if (event.target === modal) {
        closeModal();
    }
}
