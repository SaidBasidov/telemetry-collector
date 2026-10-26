import assert from 'node:assert/strict'

const baseUrl = 'http://127.0.0.1:8080'

// Отдельные тестовые датчики вне диапазона фонового сборщика.
const tagId = 1_000_000_000 + Math.floor(Math.random() * 100_000_000)
const start = Date.now()

function createRecords(id, count) {
  return Array.from({ length: count }, (_, index) => ({
    tagId: id,
    value: index,
    quality: 1,
    timestamp: new Date(start + index).toISOString(),
  }))
}

async function sendBatch(records) {
  return fetch(`${baseUrl}/api/telemetry/batch`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(records),
  })
}

async function getHistory(id) {
  const parameters = new URLSearchParams({
    from: new Date(start).toISOString(),
    to: new Date(start + 2000).toISOString(),
    limit: '1001',
  })

  const response = await fetch(
    `${baseUrl}/api/tags/${id}/history?${parameters}`
  )

  assert.equal(response.status, 200, 'Не удалось получить историю')
  return response.json()
}

try {
  const validResponse = await sendBatch(createRecords(tagId, 1000))

  assert.equal(validResponse.status, 200, 'Пачка из 1000 должна приниматься')
  assert.equal((await validResponse.json()).saved, 1000)
  assert.equal((await getHistory(tagId)).length, 1000)

  console.log('OK: 1000 измерений приняты и прочитаны из базы.')

  const oversizedResponse = await sendBatch(createRecords(tagId + 1, 1001))

  assert.equal(oversizedResponse.status, 400, 'Пачка из 1001 должна отклоняться')
  assert.equal((await getHistory(tagId + 1)).length, 0)

  console.log('OK: 1001 измерение отклонено, записей в базе нет.')

  const emptyResponse = await sendBatch([])

  assert.equal(emptyResponse.status, 400, 'Пустая пачка должна отклоняться')

  console.log('OK: пустая пачка отклонена.')
} catch (error) {
  console.error('Проверка не пройдена:', error.message)
  process.exitCode = 1
}