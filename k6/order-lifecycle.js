import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const BASE_URL = __ENV.API_URL || 'http://localhost:8080';

const placeDuration    = new Trend('order_place_ms',   true);
const confirmDuration  = new Trend('order_confirm_ms', true);
const shipDuration     = new Trend('order_ship_ms',    true);
const deliverDuration  = new Trend('order_deliver_ms', true);
const cancelDuration   = new Trend('order_cancel_ms',  true);
const errorRate        = new Rate('order_errors');

export const options = {
  scenarios: {
    lifecycle: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 5  },  // aquecimento
        { duration: '2m',  target: 20 },  // rampa
        { duration: '2m',  target: 20 },  // carga sustentada
        { duration: '30s', target: 0  },  // descida
      ],
    },
  },
  thresholds: {
    http_req_duration:      ['p(95)<500', 'p(99)<1000'],
    http_req_failed:        ['rate<0.01'],
    order_errors:           ['rate<0.01'],
    order_place_ms:         ['p(95)<300'],
    order_confirm_ms:       ['p(95)<200'],
  },
};

// ---------- helpers ----------

const HEADERS = { headers: { 'Content-Type': 'application/json' } };

function randomItem() {
  return {
    productId:  `product-${Math.floor(Math.random() * 20) + 1}`,
    quantity:   Math.floor(Math.random() * 5) + 1,
    unitPrice:  parseFloat((Math.random() * 90 + 10).toFixed(2)),
  };
}

function post(path, body) {
  return http.post(`${BASE_URL}${path}`, body ? JSON.stringify(body) : null, HEADERS);
}

// ---------- etapas ----------

function placeOrder() {
  const start = Date.now();
  const res = post('/orders', {
    customerId: `customer-${Math.floor(Math.random() * 50) + 1}`,
    items: [randomItem()],
  });
  placeDuration.add(Date.now() - start);

  const ok = check(res, {
    'place → 201': (r) => r.status === 201,
    'place → id presente': (r) => !!r.json('id'),
  });
  errorRate.add(!ok);
  return ok ? res.json('id') : null;
}

function confirmOrder(id) {
  const start = Date.now();
  const res = post(`/orders/${id}/confirm`);
  confirmDuration.add(Date.now() - start);
  const ok = check(res, { 'confirm → 204': (r) => r.status === 204 });
  errorRate.add(!ok);
  return ok;
}

function shipOrder(id) {
  const start = Date.now();
  const res = post(`/orders/${id}/ship`);
  shipDuration.add(Date.now() - start);
  const ok = check(res, { 'ship → 204': (r) => r.status === 204 });
  errorRate.add(!ok);
  return ok;
}

function deliverOrder(id) {
  const start = Date.now();
  const res = post(`/orders/${id}/deliver`);
  deliverDuration.add(Date.now() - start);
  check(res, { 'deliver → 204': (r) => r.status === 204 });
}

function cancelOrder(id) {
  const reasons = ['Fora de estoque', 'Cliente desistiu', 'Pagamento recusado'];
  const reason  = reasons[Math.floor(Math.random() * reasons.length)];
  const start   = Date.now();
  const res     = post(`/orders/${id}/cancel`, { reason });
  cancelDuration.add(Date.now() - start);
  check(res, { 'cancel → 204': (r) => r.status === 204 });
}

// ---------- cenários ----------

function fullLifecycle() {
  const id = placeOrder();
  if (!id) return;
  sleep(0.2);

  if (!confirmOrder(id)) return;
  sleep(0.2);

  if (!shipOrder(id)) return;
  sleep(0.2);

  deliverOrder(id);
}

function placeThenCancel() {
  const id = placeOrder();
  if (!id) return;
  sleep(0.2);

  cancelOrder(id);
}

// ---------- VU entry point ----------

export default function () {
  // 80 % ciclo completo · 20 % cancelamento
  if (Math.random() < 0.8) {
    fullLifecycle();
  } else {
    placeThenCancel();
  }

  sleep(1);
}
