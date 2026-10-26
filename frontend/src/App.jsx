import { useState } from 'react'
import './App.css'
import HistoryPanel from './HistoryPanel'

export default function App() {
  const [tagId, setTagId] = useState('5')
  const [measurement, setMeasurement] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  async function loadLatest(event) {
    event.preventDefault()

    const id = Number(tagId)

    if (!Number.isInteger(id) || id <= 0 || id > 2147483647) {
      setError('Введите целый номер датчика от 1 до 2147483647.')
      return
    }

    setLoading(true)
    setError('')
    setMessage('')
    setMeasurement(null)

    try {
      const response = await fetch(`/api/tags/${id}/latest`)

      if (response.status === 404) {
        setMessage(`Для датчика № ${id} измерений пока нет.`)
        return
      }

      if (!response.ok) {
        throw new Error(`Не удалось получить измерение. HTTP ${response.status}`)
      }

      const data = await response.json()
      setMeasurement(data)
    } catch (error) {
      setError(error.message || 'Не удалось подключиться к серверу.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="dashboard">
      <header>
        <p className="eyebrow">Мониторинг датчиков</p>
        <h1>Телеметрия</h1>
        <p className="subtitle">
          Последние измерения из PostgreSQL
        </p>
      </header>

      <section className="panel">
        <h2>Последнее измерение</h2>

        <form className="search-form" onSubmit={loadLatest}>
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
              disabled={loading}
            />
          </label>

          <button type="submit" disabled={loading}>
            {loading ? 'Загрузка…' : 'Получить измерение'}
          </button>
        </form>

        {error && <p className="error" role="alert">{error}</p>}
        {message && <p role="status">{message}</p>}

        {measurement && (
          <div className="measurement" aria-live="polite">
            <p>Датчик № {measurement.tagId}</p>

            <p className="measurement-value">
              {measurement.value.toLocaleString('ru-RU', {
                maximumFractionDigits: 6,
              })}
            </p>

            <p>
              Качество:{' '}
              <span
                className={
                  measurement.quality === 1 ? 'quality-good' : 'quality-bad'
                }
              >
                {measurement.quality === 1 ? 'Хорошее' : 'Плохое'}
              </span>
            </p>

            <p>
              Время измерения:{' '}
              <time dateTime={measurement.timestamp}>
                {new Date(measurement.timestamp).toLocaleString('ru-RU')}
              </time>
            </p>

            <p className="hint">
              Время показано в часовом поясе браузера.
              Для обновления нажмите кнопку ещё раз.
            </p>
          </div>
        )}
      </section>
      <HistoryPanel />
    </main>
  )
}