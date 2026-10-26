import { useState } from 'react'

function toLocalInputValue(date) {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 19)
}

export default function HistoryPanel() {
  const [tagId, setTagId] = useState('5')
  const [from, setFrom] = useState(() =>
    toLocalInputValue(new Date(Date.now() - 60 * 60 * 1000))
  )
  const [to, setTo] = useState(() => toLocalInputValue(new Date()))
  const [limit, setLimit] = useState('100')
  const [records, setRecords] = useState(null)
  const [loadedTagId, setLoadedTagId] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function loadHistory(event) {
    event.preventDefault()
    setError('')
    setRecords(null)

    const id = Number(tagId)
    const count = Number(limit)
    const start = new Date(from)
    const end = new Date(to)

    if (!Number.isInteger(id) || id <= 0 || id > 2147483647) {
      setError('Введите корректный номер датчика.')
      return
    }

    if (!Number.isInteger(count) || count < 1 || count > 1000) {
      setError('Укажите лимит от 1 до 1000.')
      return
    }

    if (
      Number.isNaN(start.getTime()) ||
      Number.isNaN(end.getTime()) ||
      start > end
    ) {
      setError('Укажите корректный период: начало не позже конца.')
      return
    }

    setLoading(true)

    try {
      const parameters = new URLSearchParams({
        from: start.toISOString(),
        to: end.toISOString(),
        limit: String(count),
      })

      const response = await fetch(
        `/api/tags/${id}/history?${parameters}`
      )

      if (!response.ok) {
        throw new Error(`Не удалось загрузить историю. HTTP ${response.status}`)
      }

      const data = await response.json()
      setRecords(data)
      setLoadedTagId(id)
    } catch (error) {
      setError(error.message || 'Не удалось подключиться к серверу.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="panel">
      <h2>История измерений</h2>

      <p className="hint">
        Выберите период в часовом поясе браузера.
        По умолчанию указан последний час на момент открытия страницы.
      </p>

      <form onSubmit={loadHistory}>
        <fieldset className="history-fields" disabled={loading}>
          <legend className="visually-hidden">Параметры истории</legend>

          <label>
            Номер датчика
            <input
              type="number"
              min="1"
              max="2147483647"
              step="1"
              required
              value={tagId}
              onChange={(event) => setTagId(event.target.value)}
            />
          </label>

          <label>
            Начало периода
            <input
              type="datetime-local"
              step="1"
              required
              value={from}
              onChange={(event) => setFrom(event.target.value)}
            />
          </label>

          <label>
            Конец периода
            <input
              type="datetime-local"
              step="1"
              required
              value={to}
              onChange={(event) => setTo(event.target.value)}
            />
          </label>

          <label>
            Лимит записей
            <input
              type="number"
              min="1"
              max="1000"
              step="1"
              required
              value={limit}
              onChange={(event) => setLimit(event.target.value)}
            />
          </label>

          <button type="submit">
            {loading ? 'Загрузка…' : 'Загрузить историю'}
          </button>
        </fieldset>
      </form>

      {error && <p className="error" role="alert">{error}</p>}

      {records !== null && (
        <>
          <p role="status">
            Датчик № {loadedTagId}. Получено записей: {records.length}.
          </p>

          {records.length === 0 ? (
            <p>За выбранный период измерений нет.</p>
          ) : (
            <div className="table-wrapper">
              <table>
                <caption>
                  Измерения от новых к старым. Время местное.
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Время</th>
                    <th scope="col">Значение</th>
                    <th scope="col">Качество</th>
                  </tr>
                </thead>
                <tbody>
                  {records.map((record, index) => (
                    <tr key={`${record.timestamp}-${index}`}>
                      <td>
                        {new Date(record.timestamp).toLocaleString('ru-RU')}
                      </td>
                      <td>
                        {record.value.toLocaleString('ru-RU', {
                          maximumFractionDigits: 6,
                        })}
                      </td>
                      <td
                        className={
                          record.quality === 1
                            ? 'quality-good'
                            : 'quality-bad'
                        }
                      >
                        {record.quality === 1 ? 'Хорошее' : 'Плохое'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </section>
  )
}